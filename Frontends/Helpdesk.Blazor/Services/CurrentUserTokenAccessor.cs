using Microsoft.AspNetCore.Components.Authorization;

namespace Helpdesk.Blazor.Services;

/// <summary>
/// Reads the JWT this circuit's auth cookie carries as a custom claim - the browser only ever
/// sees the cookie, never the raw token. One instance per circuit/request scope.
/// </summary>
public class CurrentUserTokenAccessor(AuthenticationStateProvider authStateProvider)
{
    public const string TokenClaimType = "helpdesk_token";

    /// <summary>Assignment.API's Technician.Id for this user, if they're a Technician - see
    /// User.TechnicianId. Written onto the cookie principal at login (Login.razor); nothing
    /// currently reads it back out client-side (the server derives it from the JWT itself now -
    /// see TicketController.GetCallerScope), but Login.razor still needs the constant to write it.</summary>
    public const string TechnicianIdClaimType = "helpdesk_technician_id";

    public async Task<string?> GetTokenAsync()
    {
        var state = await authStateProvider.GetAuthenticationStateAsync();
        return state.User.FindFirst(TokenClaimType)?.Value;
    }
}
