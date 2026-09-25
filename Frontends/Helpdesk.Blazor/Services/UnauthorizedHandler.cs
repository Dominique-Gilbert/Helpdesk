using System.Net;
using Microsoft.AspNetCore.Components;

namespace Helpdesk.Blazor.Services;

/// <summary>
/// A 401 from the gateway means this circuit's JWT is no longer valid (expired, or invalidated
/// by a backend restart) - the cookie session is stale, so every gateway-backed repo routes its
/// responses through here to drop it immediately rather than let a page sit and show the raw
/// 401 as an inline error.
/// </summary>
public class UnauthorizedHandler(NavigationManager navigation)
{
    public void RedirectIfUnauthorized(HttpResponseMessage response)
    {
        if (response.StatusCode != HttpStatusCode.Unauthorized) return;

        navigation.NavigateTo("/logout", forceLoad: true);
    }
}
