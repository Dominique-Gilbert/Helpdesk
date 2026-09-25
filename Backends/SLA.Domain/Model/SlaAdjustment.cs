using Helpdesk.Contracts;

namespace Helpdesk.Sla.Domain.Model;

/// <summary>
/// One admin-made shift of a clock's due time - positive minutes extend it, negative shrink
/// it. Reason is mandatory (enforced in SlaService, not here) so every change is auditable:
/// this row is the only place that fact lives once the due time itself has moved on.
/// </summary>
public class SlaAdjustment : IAuditItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SlaClockId { get; set; }
    public SlaClock? SlaClock { get; set; }

    public int DeltaMinutes { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string AdjustedBy { get; set; } = string.Empty;
    public DateTime AdjustedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
