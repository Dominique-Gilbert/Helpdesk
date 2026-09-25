namespace Helpdesk.Gateway.Dto;

public class DtoClient
{
    public Guid id { get; set; }
    public string company { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string contactNumber { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public string aboutInfo { get; set; } = string.Empty;

    /// <summary>White-labeling - both optional, null means "use the app's own default look".</summary>
    public string? primaryColor { get; set; }
    public string? logoUrl { get; set; }

    public DateTime createdAtUtc { get; set; }
    public DateTime? updatedAtUtc { get; set; }
}

/// <summary>
/// The slim, non-sensitive subset of a Client any authenticated user linked to it may read
/// about their own tenant - not the full DtoClient (Email/ContactNumber/Description/AboutInfo
/// stay BaseRole-only, via DtoClient/ClientController). Returned by IUserService.GetMyBrandingAsync.
/// </summary>
public class DtoClientBranding
{
    public Guid clientId { get; set; }
    public string displayName { get; set; } = string.Empty;
    public string? primaryColor { get; set; }
    public string? logoUrl { get; set; }
}

public class DtoClientPage
{
    public List<DtoClient> items { get; set; } = [];
    public int total { get; set; }
    public int page { get; set; }
    public int pageSize { get; set; }
}
