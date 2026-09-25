namespace Helpdesk.Gateway.Dto;

public class DtoLoginResult
{
    public string token { get; set; } = string.Empty;
    public DateTime expiresAtUtc { get; set; }
    public Guid userId { get; set; }
    public string fullName { get; set; } = string.Empty;
    public string role { get; set; } = string.Empty;
    public Guid? technicianId { get; set; }
}

public class DtoUserSummary
{
    public Guid id { get; set; }
    public string username { get; set; } = string.Empty;
    public string fullName { get; set; } = string.Empty;
    public string? email { get; set; }
    public string role { get; set; } = string.Empty;
    public bool isActive { get; set; }
    public Guid? technicianId { get; set; }

    /// <summary>The Client this User belongs to, if any - never set for BaseRole. Only BaseRole
    /// can set or change it; an Admin's own Users always inherit the creating Admin's own
    /// clientId and cannot move it afterwards. See Gateway's UserService for the enforcement.</summary>
    public Guid? clientId { get; set; }
}
