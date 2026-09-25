using Helpdesk.Contracts;

namespace Helpdesk.Assignments.Domain.Model;

public class Technician : IAuditItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty;
    public List<string> Skills { get; set; } = [];
    public List<TechnicianCategoryLevel> CategoryLevels { get; set; } = [];

    public int MaxConcurrent { get; set; } = 5;
    public int ActiveAssignments { get; set; }
    public bool IsAvailable { get; set; } = true;

    /// <summary>The linked User's Client - resolved and kept in sync by the Gateway at
    /// create/update time, never client-supplied here. Null if the linked User has no Client.</summary>
    public Guid? ClientId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public bool HasCapacity => IsAvailable && ActiveAssignments < MaxConcurrent;
}
