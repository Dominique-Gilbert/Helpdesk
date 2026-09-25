namespace Helpdesk.Contracts.Events;

/// <summary>
/// Published by SLA.API when a running clock passes its due time. Ticket.API consumes it
/// and flags the ticket - this is the stretch goal that closes the loop.
/// </summary>
public record SlaBreached
{
    public Guid TicketId { get; init; }
    public Guid SlaClockId { get; init; }
    public string Priority { get; init; } = string.Empty;
    public DateTime DueAtUtc { get; init; }
    public DateTime BreachedAtUtc { get; init; }
}
