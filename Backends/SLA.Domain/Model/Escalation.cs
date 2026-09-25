using Helpdesk.Contracts;

namespace Helpdesk.Sla.Domain.Model;

public class Escalation : IAuditItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SlaClockId { get; set; }
    public SlaClock? SlaClock { get; set; }

    /// <summary>1 on first breach, then one level per full target period overdue.</summary>
    public int Level { get; set; } = 1;

    public string Notes { get; set; } = string.Empty;
    public DateTime RaisedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
