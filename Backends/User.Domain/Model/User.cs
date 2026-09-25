using Helpdesk.Contracts;

namespace Helpdesk.Users.Domain.Model;

public class User : IAuditItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Assignment.API's Technician.Id - a different database, so this can't be a real foreign
    /// key, just a stored reference. Only set for Technician-role users; it's what "My Tickets"
    /// actually filters on instead of matching names between two unrelated tables.
    /// </summary>
    public Guid? TechnicianId { get; set; }

    /// <summary>
    /// Client.API's Client.Id - a different database, so this can't be a real foreign key
    /// either, same reasoning as TechnicianId. Never set for a BaseRole user (BaseRole belongs
    /// to no Client - it's the platform owner, not a tenant). Only BaseRole may set or change
    /// this; an Admin who creates a User always has it inherited from their own ClientId, and
    /// cannot change it afterwards - see the Gateway's UserService for where that rule lives.
    /// </summary>
    public Guid? ClientId { get; set; }

    /// <summary>Every user has exactly one role - see UserRole's unique index on UserId.</summary>
    public UserRole? UserRole { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
