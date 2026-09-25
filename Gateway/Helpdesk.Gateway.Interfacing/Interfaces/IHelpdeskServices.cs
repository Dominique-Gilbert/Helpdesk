using Helpdesk.Gateway.Dto;

namespace Helpdesk.Gateway.Interfacing.Interfaces;

/// <summary>
/// Who is asking, for the visibility check every ITicketService method (other than Create)
/// applies before returning or mutating a ticket: Admin/Support/Technician only ever see their
/// own Client's tickets (Support/Technician further narrowed to their own requestedByUserId /
/// Assignment.API-confirmed tickets); BaseRole sees every Client's, or one specific Client's via
/// ClientFilter (the profile-menu switcher - see BaseRoleViewContext on the frontend).
/// Built by the controller from the caller's own validated JWT claims, never from client input -
/// same discipline as UserController.CallerId.
/// </summary>
public readonly record struct TicketAccessScope(Guid CallerId, string Role, Guid? TechnicianId, Guid? ClientId, Guid? ClientFilter = null)
{
    public static TicketAccessScope Admin(Guid callerId) => new(callerId, "Admin", null, null);
}

public interface ITicketService
{
    Task<DtoTicket> CreateAsync(CreateTicketDto request, CancellationToken ct = default);
    Task<DtoTicket?> GetAsync(Guid id, TicketAccessScope scope, CancellationToken ct = default);
    Task<DtoTicketPage> ListAsync(string? status, string? priority, int page, int pageSize, TicketAccessScope scope, CancellationToken ct = default);
    Task<DtoTicket?> UpdateAsync(Guid id, UpdateTicketDto request, TicketAccessScope scope, CancellationToken ct = default);
    Task<DtoTicket?> CloseAsync(Guid id, TicketAccessScope scope, CancellationToken ct = default);
    Task<DtoTicket?> AddCommentAsync(Guid id, AddCommentDto request, TicketAccessScope scope, CancellationToken ct = default);
}

/// <summary>Same shape and rule as TicketAccessScope, minus the Support/Technician-specific
/// narrowing Assets/Technicians have no equivalent of (both are Admin/BaseRole-only areas):
/// Admin sees only their own Client's records; BaseRole sees every Client's, or one specific
/// Client's via ClientFilter.</summary>
public readonly record struct TenantScope(Guid CallerId, string Role, Guid? ClientId, Guid? ClientFilter = null);

public interface IAssetService
{
    Task<DtoAsset> CreateAsync(CreateAssetDto request, CancellationToken ct = default);
    Task<DtoAsset?> GetAsync(Guid id, TenantScope scope, CancellationToken ct = default);
    Task<DtoAssetPage> ListAsync(string? status, string? type, int page, int pageSize, TenantScope scope, CancellationToken ct = default);
    Task<DtoAsset?> UpdateAsync(Guid id, UpdateAssetDto request, TenantScope scope, CancellationToken ct = default);
    Task<DtoAsset?> AssignAsync(Guid id, AssignAssetDto request, TenantScope scope, CancellationToken ct = default);
    Task<DtoAsset?> ReturnAsync(Guid id, string? notes, TenantScope scope, CancellationToken ct = default);
}

public interface IAssignmentService
{
    Task<IReadOnlyList<DtoTechnician>> ListTechniciansAsync(string? team, bool onlyWithCapacity, TenantScope scope, CancellationToken ct = default);
    Task<DtoTechnician?> GetTechnicianAsync(Guid id, TenantScope scope, CancellationToken ct = default);
    Task<DtoTechnician> CreateTechnicianAsync(CreateTechnicianDto request, CancellationToken ct = default);
    Task<DtoTechnician?> UpdateTechnicianAsync(Guid id, UpdateTechnicianDto request, CancellationToken ct = default);
    Task<IReadOnlyList<DtoRoutingRule>> ListRoutingRulesAsync(bool onlyActive, TenantScope scope, CancellationToken ct = default);
    Task<DtoRoutingRule> CreateRoutingRuleAsync(CreateRoutingRuleDto request, TenantScope scope, CancellationToken ct = default);
    Task<DtoRoutingRule?> UpdateRoutingRuleAsync(Guid id, UpdateRoutingRuleDto request, TenantScope scope, CancellationToken ct = default);
    Task<DtoAssignment?> GetForTicketAsync(Guid ticketId, CancellationToken ct = default);
    Task<IReadOnlyList<DtoAssignment>> ListAssignmentsAsync(Guid? technicianId, int page, int pageSize, TenantScope scope, CancellationToken ct = default);
    /// <summary>Admin visibility into tickets level routing could not place anywhere up to
    /// Critical - see Assignment.Domain.Routing.LevelRoutingMatcher.</summary>
    Task<IReadOnlyList<DtoBacklogEntry>> ListBacklogAsync(string? category, CancellationToken ct = default);
}

public interface ISlaService
{
    /// <summary>Scoped the same way ITicketService.GetAsync is - null both when no clock exists
    /// yet and when the ticket exists but isn't visible to this caller, so the two cases stay
    /// indistinguishable from the outside.</summary>
    Task<DtoSlaClock?> GetForTicketAsync(Guid ticketId, TicketAccessScope scope, CancellationToken ct = default);
    /// <summary>Admin gets every clock; Technician/Support get only the clocks for tickets
    /// ITicketService.ListAsync would also return them - the Tickets page's own "My Tickets" SLA
    /// chips need this to not leak every other ticket's status in the same response.</summary>
    Task<IReadOnlyList<DtoSlaClock>> ListAsync(string? status, int page, int pageSize, TicketAccessScope scope, CancellationToken ct = default);
    Task<IReadOnlyList<DtoEscalation>> ListEscalationsAsync(Guid? slaClockId, CancellationToken ct = default);
    Task<DtoSlaClock?> StopAsync(Guid ticketId, CancellationToken ct = default);
    /// <summary>adjustedBy comes from the authenticated caller's identity at the controller, not
    /// from the request body - an audit trail is only worth trusting if the "who" isn't
    /// client-supplied.</summary>
    Task<DtoSlaClock?> AdjustAsync(Guid ticketId, AdjustSlaClockDto request, string adjustedBy, CancellationToken ct = default);
}

/// <summary>Fan-out aggregation: one DTO assembled from three independent services.</summary>
public interface ITicketOverviewService
{
    Task<DtoTicketOverview?> GetAsync(Guid ticketId, TicketAccessScope scope, CancellationToken ct = default);
}

/// <summary>
/// Company/customer directory - exclusive to BaseRole, unlike every other Admin-gated service
/// here (Admin itself does not get this). See ClientController and Client.API's ClientService
/// for the enforcement, NavMenu/BaseRoleOnly for the frontend side.
/// </summary>
public interface IClientService
{
    Task<DtoClient> CreateAsync(CreateClientDto request, CancellationToken ct = default);
    Task<DtoClient?> GetAsync(Guid id, CancellationToken ct = default);
    Task<DtoClientPage> ListAsync(int page, int pageSize, CancellationToken ct = default);
    Task<DtoClient?> UpdateAsync(Guid id, UpdateClientDto request, CancellationToken ct = default);

    /// <summary>Fully anonymous - the second half of the progressive/identifier-first login
    /// screen. Never null the way GetAsync can be for a real 404: an unknown id (which here only
    /// ever comes from GetLoginBrandingAsync's own resolved ClientId, never raw client input)
    /// answers with an empty DtoClientBranding rather than throwing, same non-enumeration
    /// discipline as the rest of this pre-auth path.</summary>
    Task<DtoClientBranding> GetPublicBrandingAsync(Guid clientId, CancellationToken ct = default);

    /// <summary>Self-service, Admin-only - "the App Theme button". clientId always comes from the
    /// caller's own validated JWT (see UserService.UpdateMyBrandingAsync), never client input -
    /// backed by Client.API's UpdateClientBranding, which independently re-checks that too.</summary>
    Task<DtoClientBranding?> UpdateMyBrandingAsync(Guid clientId, UpdateMyBrandingDto request, CancellationToken ct = default);
}

/// <summary>
/// Who is asking, for List/Create/Update's own tenant rule (distinct from TicketAccessScope -
/// Users has no Technician/Support visibility split, just "which Client's Users"): BaseRole sees
/// and can reassign every User's ClientId; an Admin only ever sees Users sharing their own
/// ClientId, and any User they create inherits it automatically - they can never set or change
/// it. Built by the controller from the caller's own validated JWT claims, never from client
/// input - same discipline as TicketAccessScope/CallerScopeExtensions.
/// </summary>
public readonly record struct UserCallerScope(Guid CallerId, string Role, Guid? ClientId);

/// <summary>Login is the only unauthenticated action here - List/Create/Update require the
/// Admin role, and the self-service Get/UpdateMyProfile/ChangePassword require only that the
/// caller is authenticated (the Gateway controller passes their own id, never a client-chosen
/// one - see the .proto for why).</summary>
public interface IUserService
{
    Task<DtoLoginResult?> LoginAsync(LoginRequestDto request, CancellationToken ct = default);
    Task<List<DtoUserSummary>> ListUsersAsync(UserCallerScope caller, Guid? clientFilter, CancellationToken ct = default);
    Task<DtoUserSummary> CreateUserAsync(CreateUserDto request, UserCallerScope caller, CancellationToken ct = default);
    Task<DtoUserSummary?> UpdateUserAsync(Guid id, UpdateUserDto request, UserCallerScope caller, CancellationToken ct = default);
    Task<DtoUserSummary?> GetUserAsync(Guid id, CancellationToken ct = default);
    Task<DtoUserSummary?> UpdateMyProfileAsync(Guid id, UpdateMyProfileDto request, CancellationToken ct = default);
    Task<DtoUserSummary?> ChangePasswordAsync(Guid id, ChangePasswordDto request, CancellationToken ct = default);

    /// <summary>Self-service, for white-labeling: null when the caller belongs to no Client
    /// (BaseRole, or a legacy/unlinked Admin) - the frontend falls back to its own default theme
    /// in that case, same "not an error" reasoning as everywhere else null means "nothing to
    /// show" in this codebase. Always resolves the caller's OWN ClientId from their validated JWT
    /// scope, never a client-supplied id - there is no path here to read another Client's
    /// branding.</summary>
    Task<DtoClientBranding?> GetMyBrandingAsync(UserCallerScope caller, CancellationToken ct = default);

    /// <summary>Self-service, Admin-only - "the App Theme button". Null (frontend renders as
    /// NotFound) if the caller isn't an Admin with a linked Client - the controller's own role
    /// restriction should already prevent that, this is the same "don't blindly trust the layer
    /// above" belt-and-suspenders check every self-service path here has.</summary>
    Task<DtoClientBranding?> UpdateMyBrandingAsync(UserCallerScope caller, UpdateMyBrandingDto request, CancellationToken ct = default);

    /// <summary>Also [AllowAnonymous]-reachable at the controller, alongside LoginAsync - the
    /// progressive/identifier-first login screen's pre-auth lookup. Never null the way
    /// GetMyBrandingAsync can be: an unknown/unlinked username answers with an empty
    /// DtoClientBranding (falls back to the app's own default look) rather than a distinguishable
    /// "not found", so this can never be used to enumerate usernames.</summary>
    Task<DtoClientBranding> GetLoginBrandingAsync(string username, CancellationToken ct = default);
}
