namespace Helpdesk.Contracts.Events;

/// <summary>
/// Published by Ticket.API once a ticket is committed. No addressed recipient:
/// whoever is listening, listens. Assignment.API and SLA.API each bind their own
/// queue to this message type and consume it independently of one another.
///
/// Category and priority travel as strings on purpose - the consumers must not take
/// a compile-time dependency on Ticket.Domain's enums. Each service parses them into
/// its own enum, which is why those enums are duplicated rather than shared.
/// </summary>
public record TicketCreated
{
    public Guid TicketId { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string RequestedBy { get; init; } = string.Empty;

    /// <summary>The ticket's own Client - null for a ticket raised with no Client attached.
    /// Assignment.API uses this to keep routing (technicians and rules) inside one company;
    /// SLA.API tags its clock with it for the same reason.</summary>
    public Guid? ClientId { get; init; }

    /// <summary>The comment entered at ticket creation, if any - Assignment.API matches this
    /// text against technician specialties for keyword-based routing.</summary>
    public string Comment { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }
}
