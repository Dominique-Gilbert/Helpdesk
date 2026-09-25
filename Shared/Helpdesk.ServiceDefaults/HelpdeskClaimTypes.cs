namespace Helpdesk.ServiceDefaults;

/// <summary>
/// Custom claim type for the login Username, deliberately not
/// <see cref="System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.UniqueName"/> ("unique_name") -
/// that short name gets remapped to <see cref="System.Security.Claims.ClaimTypes.Name"/> by the
/// JWT handler's default inbound claim mapping, which would collide with the FullName claim
/// (also stored under ClaimTypes.Name). A plain, unmapped string sidesteps that collision.
/// </summary>
public static class HelpdeskClaimTypes
{
    public const string Username = "username";

    /// <summary>Assignment.API's Technician.Id for this user, present only when they're a
    /// Technician. Same claim name the Blazor client already uses for its own cookie-based
    /// principal (see CurrentUserTokenAccessor.TechnicianIdClaimType) - minting it onto the JWT
    /// itself means the Gateway (and any backend) can read a caller's technicianId directly off
    /// the validated token, the same way it already reads their role, instead of needing an
    /// extra User.API round trip per request.</summary>
    public const string TechnicianId = "helpdesk_technician_id";

    /// <summary>Client.API's Client.Id this user's tenant is scoped to, present only when set -
    /// never for a BaseRole user. Minted onto the JWT the same way TechnicianId is, so the
    /// Gateway can read a caller's own ClientId directly off the validated token (see
    /// CallerScopeExtensions.GetUserCallerScope) to enforce who can see/create/reassign which
    /// Users belong to which Client.</summary>
    public const string ClientId = "helpdesk_client_id";
}
