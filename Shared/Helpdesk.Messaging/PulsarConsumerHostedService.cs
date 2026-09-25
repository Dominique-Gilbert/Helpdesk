using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pulsar.Client.Api;
using Pulsar.Client.Common;

namespace Helpdesk.Messaging;

/// <summary>
/// One of these per registered consumer. A fresh DI scope (and so a fresh TConsumer, and a
/// fresh DbContext) per message - matches how MassTransit used to hand each consumed message
/// its own consumer instance.
///
/// Failures are logged and negative-acknowledged (Pulsar redelivers) rather than left
/// unacknowledged and silently retried forever, which is what a bare receive-and-forget loop
/// would do.
/// </summary>
internal sealed class PulsarConsumerHostedService<TMessage, TConsumer>(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<PulsarConsumerHostedService<TMessage, TConsumer>> logger) : BackgroundService
    where TMessage : class
    where TConsumer : class, IEventConsumer<TMessage>
{
    private readonly string _topic = PulsarNaming.TopicFor(typeof(TMessage));
    private readonly string _subscription = PulsarNaming.SubscriptionFor(typeof(TConsumer));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var serviceUrl = Environment.GetEnvironmentVariable("PULSAR_SERVICE_URL")
            ?? configuration["Pulsar:ServiceUrl"]
            ?? throw new InvalidOperationException("PULSAR_SERVICE_URL is not configured.");

        PulsarClient? client = null;
        IConsumer<byte[]>? consumer = null;

        try
        {
            // The broker can be reachable before the AppHost's init container has finished
            // creating the tenant/namespace/topic - retry rather than let a transient
            // "namespace not found" take the whole host down (BackgroundServiceExceptionBehavior
            // defaults to StopHost). Same shape as Helpdesk.Persistence.MigrationExtensions.
            const int maxAttempts = 15;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    client = await new PulsarClientBuilder().ServiceUrl(serviceUrl).BuildAsync();
                    consumer = await client.NewConsumer()
                        .Topic(_topic)
                        .SubscriptionName(_subscription)
                        .SubscriptionType(SubscriptionType.Shared)
                        .SubscriptionInitialPosition(SubscriptionInitialPosition.Earliest)
                        .SubscribeAsync();
                    break;
                }
                catch (Exception ex) when (attempt < maxAttempts)
                {
                    logger.LogWarning(ex, "Subscribe attempt {Attempt}/{MaxAttempts} for {Consumer} failed; retrying",
                        attempt, maxAttempts, typeof(TConsumer).Name);
                    if (client is not null)
                    {
                        await client.CloseAsync();
                        client = null;
                    }
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                }
            }

            // Every attempt failed (the loop above only ever `break`s on success) - without this,
            // `consumer` stays null and the receive loop below throws an unhandled NRE that hides
            // the real "couldn't reach Pulsar" cause behind a useless stack trace.
            if (consumer is null)
            {
                throw new InvalidOperationException(
                    $"{typeof(TConsumer).Name} failed to subscribe to {_topic} after {maxAttempts} attempts.");
            }

            logger.LogInformation("{Consumer} subscribed to {Topic} as {Subscription}",
                typeof(TConsumer).Name, _topic, _subscription);

            while (!stoppingToken.IsCancellationRequested)
            {
                Message<byte[]> received;
                try
                {
                    received = await consumer.ReceiveAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                try
                {
                    var payload = JsonSerializer.Deserialize<TMessage>(received.Data)
                        ?? throw new InvalidOperationException($"Deserialized {typeof(TMessage).Name} was null.");

                    using var scope = scopeFactory.CreateScope();
                    var consumerInstance = scope.ServiceProvider.GetRequiredService<TConsumer>();
                    await consumerInstance.ConsumeAsync(payload, stoppingToken);

                    await consumer.AcknowledgeAsync(received.MessageId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{Consumer} failed to process a {Message}; message will be redelivered",
                        typeof(TConsumer).Name, typeof(TMessage).Name);
                    await consumer.NegativeAcknowledge(received.MessageId);
                }
            }
        }
        finally
        {
            if (consumer is not null) await consumer.DisposeAsync();
            if (client is not null) await client.CloseAsync();
        }
    }
}
