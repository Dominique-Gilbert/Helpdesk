using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Helpdesk.Gateway.Interfacing.Interfaces;
using Helpdesk.ServiceDefaults;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Gateway.Api.Controllers;

/// <summary>
/// Shared by every controller that needs to build a TicketAccessScope from the caller's own
/// validated JWT claims (Ticket, SLA) - one extraction, not one copy per controller. Same
/// discipline as UserController.CallerId: derived from the token, never from client input.
/// </summary>
internal static class CallerScopeExtensions
{
    public static TicketAccessScope GetCallerScope(this ControllerBase controller, Guid? clientFilter = null)
    {
        var user = controller.User;

        // .NET's JwtBearer handler maps the "sub" claim to ClaimTypes.NameIdentifier by default,
        // but that mapping is version/config-dependent - checking the raw "sub" too makes this
        // robust either way rather than betting on which behavior is currently in effect.
        var callerId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var role = user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var technicianId = Guid.TryParse(user.FindFirstValue(HelpdeskClaimTypes.TechnicianId), out var id) ? id : (Guid?)null;
        var clientId = Guid.TryParse(user.FindFirstValue(HelpdeskClaimTypes.ClientId), out var cid) ? cid : (Guid?)null;

        return new TicketAccessScope(callerId, role, technicianId, clientId, clientFilter);
    }

    public static UserCallerScope GetUserCallerScope(this ControllerBase controller)
    {
        var user = controller.User;

        var callerId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var role = user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var clientId = Guid.TryParse(user.FindFirstValue(HelpdeskClaimTypes.ClientId), out var id) ? id : (Guid?)null;

        return new UserCallerScope(callerId, role, clientId);
    }

    /// <summary>Same claims as GetCallerScope, shaped for Asset/Technician's simpler Admin/BaseRole-
    /// only visibility rule (no Support/Technician narrowing, so no TechnicianId).</summary>
    public static TenantScope GetTenantScope(this ControllerBase controller, Guid? clientFilter = null)
    {
        var user = controller.User;

        var callerId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var role = user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var clientId = Guid.TryParse(user.FindFirstValue(HelpdeskClaimTypes.ClientId), out var cid) ? cid : (Guid?)null;

        return new TenantScope(callerId, role, clientId, clientFilter);
    }
}
