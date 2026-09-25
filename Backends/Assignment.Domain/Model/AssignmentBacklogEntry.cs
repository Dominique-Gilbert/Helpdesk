using Helpdesk.Contracts;

namespace Helpdesk.Assignments.Domain.Model;

/// <summary>
/// A ticket that neither matcher could place anywhere at creation time - every eligible
/// technician was fully loaded (RoutingPath.Rule), or nobody from the starting tier to
/// Critical had spare capacity (RoutingPath.Level). Waits here until BacklogDrainService
/// sweeps the queue - triggered by a ticket closing (frees a slot), a technician being
/// created or updated (gains capacity or a level that now covers this category), so a
/// backlog entry only ever waits as long as it takes for somewhere to actually open up.
/// </summary>
public class AssignmentBacklogEntry : IAuditItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TicketId { get; set; }
    public string TicketReference { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;
    public TicketPriority Priority { get; set; } = TicketPriority.Normal;
    public SkillLevel StartingLevel { get; set; } = SkillLevel.Normal;
    public string CommentText { get; set; } = string.Empty;
    public RoutingPath RoutingPath { get; set; } = RoutingPath.Level;

    /// <summary>The ticket's own Client - carried from Ticket.Created so BacklogDrainService can
    /// keep this entry inside the same company when it re-tries a matcher later. Technicians are
    /// scoped strictly (only their own company's entries); RoutingPath.Rule entries also accept
    /// a global (null-ClientId) rule, same as at creation time.</summary>
    public Guid? ClientId { get; set; }

    public DateTime QueuedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
