namespace Helpdesk.Gateway.Dto;

public class CreateTicketDto
{
    public string title { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public string category { get; set; } = "General";
    public string priority { get; set; } = "Normal";
    public string requestedBy { get; set; } = string.Empty;

    /// <summary>
    /// Optional first comment, posted at creation time. Two jobs: it becomes a real Comment on
    /// the ticket like any other, and Assignment.API reads its text for keyword-based routing
    /// (a technician's Skills are searched against it) - the only text that exists at the exact
    /// moment a ticket is routed, since routing happens before anyone else gets a chance to
    /// comment.
    /// </summary>
    public string comment { get; set; } = string.Empty;
}

public class UpdateTicketDto
{
    public string? title { get; set; }
    public string? description { get; set; }
    public string? status { get; set; }
    public string? priority { get; set; }
}

public class AddCommentDto
{
    public string author { get; set; } = string.Empty;
    public string body { get; set; } = string.Empty;
    public bool isInternal { get; set; }
}

public class CreateAssetDto
{
    public string tag { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public string serialNumber { get; set; } = string.Empty;
    public string type { get; set; } = "Laptop";
    public string location { get; set; } = string.Empty;
}

public class UpdateAssetDto
{
    public string? name { get; set; }
    public string? serialNumber { get; set; }
    public string? location { get; set; }
    public string? type { get; set; }
    public string? status { get; set; }
}

public class CreateClientDto
{
    public string company { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string contactNumber { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public string aboutInfo { get; set; } = string.Empty;
    public string? primaryColor { get; set; }
    public string? logoUrl { get; set; }
}

/// <summary>Self-service, Admin-only - "the App Theme button" (see UserController.UpdateMyBranding).
/// Deliberately only these fields - unlike UpdateClientDto, there is no way for an Admin to touch
/// their own Client's Company/Email/ContactNumber/Description/AboutInfo through this. displayName
/// is a branding-only override of Company, not a rename - empty/null clears it back to Company.</summary>
public class UpdateMyBrandingDto
{
    public string? displayName { get; set; }
    public string? primaryColor { get; set; }
    public string? logoUrl { get; set; }
}

public class UpdateClientDto
{
    public string? company { get; set; }
    public string? name { get; set; }
    public string? email { get; set; }
    public string? contactNumber { get; set; }
    public string? description { get; set; }
    public string? aboutInfo { get; set; }
    public string? primaryColor { get; set; }
    public string? logoUrl { get; set; }
}

public class CreateRoutingRuleDto
{
    public string name { get; set; } = string.Empty;
    public string? category { get; set; }
    public string minPriority { get; set; } = "Low";
    public string team { get; set; } = string.Empty;
    public int rank { get; set; } = 50;

    /// <summary>Same rule as CreateTechnicianDto.clientId - resolved server-side from the
    /// caller, except for BaseRole who may pick any Client (or none, for a global rule).</summary>
    public Guid? clientId { get; set; }
}

public class UpdateRoutingRuleDto
{
    public string name { get; set; } = string.Empty;
    public string? category { get; set; }
    public string minPriority { get; set; } = "Low";
    public string team { get; set; } = string.Empty;
    public int rank { get; set; } = 50;
    public bool isActive { get; set; } = true;

    /// <summary>Same rule as CreateRoutingRuleDto.clientId.</summary>
    public Guid? clientId { get; set; }
}

public class AssignAssetDto
{
    public Guid? ticketId { get; set; }
    public Guid? technicianId { get; set; }
    public string assignedTo { get; set; } = string.Empty;
    public string notes { get; set; } = string.Empty;
}

public class LoginRequestDto
{
    public string username { get; set; } = string.Empty;
    public string password { get; set; } = string.Empty;
}

/// <summary>
/// One (category, level) pairing - e.g. "Hardware" at "High". A technician can hold several of
/// these, one per category they work; that per-category level (not Team) is what the routing
/// algorithm actually escalates through. See Assignment.Domain.Routing.LevelRoutingMatcher.
/// </summary>
public class DtoTechnicianCategoryLevel
{
    public string category { get; set; } = string.Empty;
    public string level { get; set; } = "Normal";
}

/// <summary>
/// The Assignment.API-side fields a Technician-role User needs. Creating such a User always
/// creates a brand-new Technician alongside it (see Gateway's UserService orchestration) -
/// there is no "link an existing technician" path.
/// </summary>
public class TechnicianDetailsDto
{
    public string email { get; set; } = string.Empty;
    public string team { get; set; } = string.Empty;
    public List<string> skills { get; set; } = [];
    public int maxConcurrent { get; set; } = 5;
    public bool isAvailable { get; set; } = true;
    public List<DtoTechnicianCategoryLevel> categoryLevels { get; set; } = [];
}

public class CreateUserDto
{
    public string username { get; set; } = string.Empty;
    public string password { get; set; } = string.Empty;
    public string fullName { get; set; } = string.Empty;
    public string role { get; set; } = string.Empty;

    /// <summary>Required when role == "Technician" - drives the Technician record created alongside this User.</summary>
    public TechnicianDetailsDto? technician { get; set; }

    /// <summary>
    /// Only honored when the caller is BaseRole - an Admin's choice here is ignored server-side;
    /// their new User always inherits the creating Admin's own clientId instead. See Gateway's
    /// UserService.CreateUserAsync.
    /// </summary>
    public Guid? clientId { get; set; }
}

public class UpdateUserDto
{
    public string fullName { get; set; } = string.Empty;
    public string role { get; set; } = string.Empty;
    public bool isActive { get; set; } = true;

    /// <summary>
    /// The User's current linked Technician, if any (echoed back from DtoUserSummary.technicianId).
    /// When role == "Technician": present -> that Technician's details are updated; absent -> a
    /// new Technician is created and linked (e.g. the role just changed to Technician).
    /// </summary>
    public Guid? technicianId { get; set; }

    /// <summary>Required when role == "Technician".</summary>
    public TechnicianDetailsDto? technician { get; set; }

    /// <summary>Empty/null leaves the current password unchanged.</summary>
    public string? newPassword { get; set; }

    /// <summary>
    /// Only honored when the caller is BaseRole - anyone else's choice here is ignored
    /// server-side, and the User's existing clientId is left untouched. See Gateway's
    /// UserService.UpdateUserAsync.
    /// </summary>
    public Guid? clientId { get; set; }
}

/// <summary>Self-service - deliberately has no Role/IsActive/TechnicianId (see UpdateUserDto
/// for the admin-only equivalent of those).</summary>
public class UpdateMyProfileDto
{
    public string fullName { get; set; } = string.Empty;
    public string? email { get; set; }
}

/// <summary>Self-service - unlike UpdateUserDto.newPassword, this requires the caller's
/// current password.</summary>
public class ChangePasswordDto
{
    public string currentPassword { get; set; } = string.Empty;
    public string newPassword { get; set; } = string.Empty;
}

public class CreateTechnicianDto
{
    public string fullName { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string team { get; set; } = string.Empty;
    public List<string> skills { get; set; } = [];
    public int maxConcurrent { get; set; } = 5;
    public List<DtoTechnicianCategoryLevel> categoryLevels { get; set; } = [];

    /// <summary>Internal - set by Gateway's UserService orchestration to the linked User's own
    /// resolved ClientId, never taken from the standalone endpoint's own request body caller.</summary>
    public Guid? clientId { get; set; }
}

public class UpdateTechnicianDto
{
    public string fullName { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string team { get; set; } = string.Empty;
    public List<string> skills { get; set; } = [];
    public int maxConcurrent { get; set; } = 5;
    public bool isAvailable { get; set; } = true;
    public List<DtoTechnicianCategoryLevel> categoryLevels { get; set; } = [];

    /// <summary>Same rule as CreateTechnicianDto.clientId.</summary>
    public Guid? clientId { get; set; }
}

/// <summary>Whole minutes, positive to extend or negative to shrink. Delta, not an absolute
/// due time, so every change reads naturally in the audit trail ("+240: customer requested
/// reschedule") without anyone needing to know what the due time was before.</summary>
public class AdjustSlaClockDto
{
    public int deltaMinutes { get; set; }
    public string reason { get; set; } = string.Empty;
}
