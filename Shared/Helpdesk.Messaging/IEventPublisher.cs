namespace Helpdesk.Messaging;

/// <summary>
/// Publishes an event with no addressed recipient - whoever is subscribed, gets it.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<TMessage>(TMessage message, CancellationToken ct = default) where TMessage : class;
}
