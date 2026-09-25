using Helpdesk.Contracts;

namespace Helpdesk.Assignments.Domain.Model;

/// <summary>
/// A technician's skill tier for one category, e.g. High in Hardware but Low in Network.
/// One row per (TechnicianId, Category) pair - enforced by a unique index in AssignmentContext.
/// A category with no rows for any technician has no level-routed technicians at all, which is
/// how LevelRoutingMatcher decides to fall back to the legacy RoutingRuleMatcher instead.
/// </summary>
public class TechnicianCategoryLevel : IAuditItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TechnicianId { get; set; }
    public Technician? Technician { get; set; }

    public string Category { get; set; } = string.Empty;
    public SkillLevel Level { get; set; } = SkillLevel.Normal;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
