namespace Helpdesk.Messaging;

/// <summary>
/// Reacts to one message type. Register with AddPulsarConsumer&lt;TMessage, TConsumer&gt;() -
/// the consumer's own type name becomes its Pulsar subscription name, so two consumers of the
/// same message type never collide (see PulsarNaming).
/// </summary>
public interface IEventConsumer<TMessage>
{
    Task ConsumeAsync(TMessage message, CancellationToken ct);
}
