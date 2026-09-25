using Helpdesk.Contracts;

namespace Helpdesk.Assignments.Domain.Model;

public class RoutingRule : IAuditItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;

    /// <summary>Null or empty means "any category" - the catch-all rules.</summary>
    public string? Category { get; set; }

    /// <summary>The rule applies to tickets at this priority or above.</summary>
    public TicketPriority MinPriority { get; set; } = TicketPriority.Low;

    public string Team { get; set; } = string.Empty;

    /// <summary>Lower rank is evaluated first within the same specificity band.</summary>
    public int Rank { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Null = visible to every Client (a platform-wide fallback rule). Same rule as
    /// Technician.ClientId: server-resolved from the caller, never trusted from client input
    /// for anyone but BaseRole.</summary>
    public Guid? ClientId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
