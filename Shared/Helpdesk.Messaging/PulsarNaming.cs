using System.Text;

namespace Helpdesk.Messaging;

/// <summary>
/// One tenant/namespace for the whole app. Topic = the message type (shared by every
/// publisher/consumer of it); subscription = the consumer type. Two consumers of the same
/// topic with different subscription names each get their own independent copy of every
/// message - that's what makes fan-out work on Pulsar, in place of MassTransit's
/// one-queue-per-consumer-type-on-one-exchange behaviour.
/// </summary>
internal static class PulsarNaming
{
    public static string TopicFor(Type messageType) =>
        $"persistent://helpdesk/events/{ToKebabCase(messageType.Name)}";

    public static string SubscriptionFor(Type consumerType) => ToKebabCase(consumerType.Name);

    private static string ToKebabCase(string value)
    {
        var sb = new StringBuilder(value.Length + 4);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsUpper(c))
            {
                if (i > 0) sb.Append('-');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}
