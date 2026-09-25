using Helpdesk.Contracts;

namespace Helpdesk.Sla.Domain.Model;

public class SlaClock : IAuditItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TicketId { get; set; }
    public string TicketReference { get; set; } = string.Empty;

    public TicketPriority Priority { get; set; } = TicketPriority.Normal;
    public SlaClockStatus Status { get; set; } = SlaClockStatus.Running;

    /// <summary>The ticket's own Client, carried from Ticket.Created - SLA.API otherwise has
    /// no notion of company at all. Not used for lookups today (a clock is always found by its
    /// unique TicketId), but keeps this clock tagged the same way Technician/RoutingRule/Ticket
    /// already are, rather than being the one place with no company boundary.</summary>
    public Guid? ClientId { get; set; }

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime DueAtUtc { get; set; }
    public DateTime? StoppedAtUtc { get; set; }
    public DateTime? BreachedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public List<Escalation> Escalations { get; set; } = [];
    public List<SlaAdjustment> Adjustments { get; set; } = [];
}
