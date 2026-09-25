using Helpdesk.Contracts;

namespace Helpdesk.Clients.Domain.Model;

/// <summary>A company/customer contact - not a User (no login), and not a Technician.</summary>
public class Client : IAuditItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Company { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AboutInfo { get; set; } = string.Empty;

    /// <summary>White-labeling - all optional. Null means "use the app's own default look for
    /// this field" (see Helpdesk.Blazor's ClientBrandingProvider / TenantTheming). DisplayName
    /// null means "use Company" - it's a branding-only override, separate from the Company name
    /// BaseRole manages on the Clients page.</summary>
    public string? DisplayName { get; set; }
    public string? PrimaryColor { get; set; }
    public string? LogoUrl { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
