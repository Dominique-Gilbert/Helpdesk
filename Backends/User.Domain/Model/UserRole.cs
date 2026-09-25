using Helpdesk.Contracts;

namespace Helpdesk.Users.Domain.Model;

/// <summary>
/// The link between a User and a Role. One row per user - enforced by a unique index on
/// UserId (see UserContext), not just by convention - everyone gets exactly one role.
/// </summary>
public class UserRole : IAuditItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid RoleId { get; set; }
    public Role? Role { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
