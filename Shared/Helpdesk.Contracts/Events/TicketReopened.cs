namespace Helpdesk.Contracts.Events;

/// <summary>
/// Published by Ticket.API when a comment lands on a ticket that was still within its 24-hour
/// post-close grace window. SLA.API consumes it and restarts the clock from scratch - a
/// reopened ticket gets a fresh full SLA window, not the remainder of the old one.
///
/// Priority travels as a string, same reasoning as Ticket.Created: SLA.API must not take a
/// compile-time dependency on Ticket.Domain's enum.
/// </summary>
public record TicketReopened
{
    public Guid TicketId { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public DateTime ReopenedAtUtc { get; init; }

    /// <summary>Same rule as TicketCreated.ClientId.</summary>
    public Guid? ClientId { get; init; }
}
