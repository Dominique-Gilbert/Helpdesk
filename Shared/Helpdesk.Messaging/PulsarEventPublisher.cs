using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Pulsar.Client.Api;

namespace Helpdesk.Messaging;

/// <summary>
/// One shared PulsarClient per process, one producer per message type (created lazily on
/// first publish and cached - a process that only ever publishes one event type never pays
/// for a second producer).
/// </summary>
internal sealed class PulsarEventPublisher(IConfiguration configuration) : IEventPublisher, IAsyncDisposable
{
    private readonly string _serviceUrl = Environment.GetEnvironmentVariable("PULSAR_SERVICE_URL")
        ?? configuration["Pulsar:ServiceUrl"]
        ?? throw new InvalidOperationException("PULSAR_SERVICE_URL is not configured.");

    private readonly SemaphoreSlim _clientLock = new(1, 1);
    private readonly ConcurrentDictionary<Type, Lazy<Task<IProducer<byte[]>>>> _producers = new();
    private PulsarClient? _client;

    public async Task PublishAsync<TMessage>(TMessage message, CancellationToken ct = default) where TMessage : class
    {
        var producer = await _producers.GetOrAdd(typeof(TMessage), _ => new Lazy<Task<IProducer<byte[]>>>(
            () => CreateProducerAsync(PulsarNaming.TopicFor(typeof(TMessage))))).Value;

        var payload = JsonSerializer.SerializeToUtf8Bytes(message);
        await producer.SendAsync(payload);
    }

    private async Task<IProducer<byte[]>> CreateProducerAsync(string topic)
    {
        var client = await GetClientAsync();
        return await client.NewProducer().Topic(topic).CreateAsync();
    }

    private async Task<PulsarClient> GetClientAsync()
    {
        if (_client is not null) return _client;

        await _clientLock.WaitAsync();
        try
        {
            return _client ??= await new PulsarClientBuilder().ServiceUrl(_serviceUrl).BuildAsync();
        }
        finally
        {
            _clientLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_client is not null)
        {
            await _client.CloseAsync();
        }
    }
}
