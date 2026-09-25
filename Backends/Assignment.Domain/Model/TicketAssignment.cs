using Helpdesk.Contracts;

namespace Helpdesk.Assignments.Domain.Model;

/// <summary>
/// The row the fan-out is proved by: one Ticket.Created produces exactly one of these.
/// The brief names Technician and RoutingRule as the entities this service owns; this is
/// the join between them and a ticket, which the routing outcome has to be recorded in.
/// </summary>
public class TicketAssignment : IAuditItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TicketId { get; set; }
    public string TicketReference { get; set; } = string.Empty;

    public Guid TechnicianId { get; set; }
    public Technician? Technician { get; set; }

    public Guid? RoutingRuleId { get; set; }
    public RoutingRule? RoutingRule { get; set; }

    public string Category { get; set; } = string.Empty;
    public TicketPriority Priority { get; set; } = TicketPriority.Normal;

    /// <summary>Why this technician - written at assignment time so the decision is auditable.</summary>
    public string Explanation { get; set; } = string.Empty;

    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
