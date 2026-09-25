using Helpdesk.Contracts;

namespace Helpdesk.Tickets.Domain.Model;

/// <summary>Plain POCO. No EF attributes, no framework references - configuration lives in TicketContext.</summary>
public class Ticket : IAuditItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Reference { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;

    /// <summary>
    /// User.API's User.Id for whoever was signed in when this ticket was created - set from the
    /// caller's JWT, never client-supplied. Null for tickets created before this field existed.
    /// This, not RequestedBy (a free-text contact name), is what a Support user's "My Tickets"
    /// filters on.
    /// </summary>
    public Guid? RequestedByUserId { get; set; }

    /// <summary>
    /// The creator's login username and role at the moment of creation - also set from the
    /// caller's JWT, also empty for tickets created before this field existed. A historical
    /// snapshot, not a live lookup: if that user's role changes later, this does not change
    /// retroactively (matching how a commit's author line doesn't change if they're renamed).
    /// </summary>
    public string RequestedByUsername { get; set; } = string.Empty;
    public string RequestedByRole { get; set; } = string.Empty;

    /// <summary>
    /// The requester's own Client at the moment of creation - set from the caller's JWT, never
    /// client-supplied, same discipline and same "historical snapshot" rule as RequestedByRole.
    /// Null for a caller with no Client (BaseRole, or a legacy/unlinked Admin) and for tickets
    /// created before this field existed. This is what scopes an Admin/Support/Technician's own
    /// view to their own company - see the Gateway's TicketService.ListAsync.
    /// </summary>
    public Guid? ClientId { get; set; }

    public TicketCategory Category { get; set; } = TicketCategory.General;
    public TicketPriority Priority { get; set; } = TicketPriority.Normal;
    public TicketStatus Status { get; set; } = TicketStatus.New;

    /// <summary>Set when SLA.API publishes SLA.Breached and this service consumes it.</summary>
    public bool SlaBreached { get; set; }
    public DateTime? SlaBreachedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public List<Comment> Comments { get; set; } = [];
}
