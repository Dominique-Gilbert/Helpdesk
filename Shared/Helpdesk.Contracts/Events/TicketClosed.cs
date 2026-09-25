namespace Helpdesk.Contracts.Events;

/// <summary>
/// Published by Ticket.API when a ticket is closed. SLA.API consumes it and stops the
/// matching clock - closing a ticket should not leave its SLA clock ticking towards a
/// breach that no longer means anything.
/// </summary>
public record TicketClosed
{
    public Guid TicketId { get; init; }
    public string Reference { get; init; } = string.Empty;
    public DateTime ClosedAtUtc { get; init; }

    /// <summary>Same rule as TicketCreated.ClientId.</summary>
    public Guid? ClientId { get; init; }
}
