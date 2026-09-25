using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Helpdesk.Messaging;

public static class MessagingExtensions
{
    /// <summary>
    /// Registers the Pulsar-backed IEventPublisher. Call once per service that publishes
    /// anything (Ticket.API, SLA.API); harmless to call even if a service only consumes.
    /// </summary>
    public static IHostApplicationBuilder AddHelpdeskPulsar(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IEventPublisher, PulsarEventPublisher>();
        return builder;
    }

    /// <summary>
    /// Registers one consumer as a hosted background service. Call once per (message,
    /// consumer) pair - e.g. AddPulsarConsumer&lt;TicketCreated, AssignmentTicketCreatedConsumer&gt;().
    /// </summary>
    public static IHostApplicationBuilder AddPulsarConsumer<TMessage, TConsumer>(this IHostApplicationBuilder builder)
        where TMessage : class
        where TConsumer : class, IEventConsumer<TMessage>
    {
        builder.Services.AddScoped<TConsumer>();
        builder.Services.AddHostedService<PulsarConsumerHostedService<TMessage, TConsumer>>();
        return builder;
    }
}
