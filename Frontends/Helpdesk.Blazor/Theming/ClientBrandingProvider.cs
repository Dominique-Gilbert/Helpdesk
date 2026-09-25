using Helpdesk.Blazor.Repositories;
using TenantTheming;

namespace Helpdesk.Blazor.Theming;

/// <summary>
/// Helpdesk's one implementation of TenantTheming's seam. Deliberately ignores its own
/// <paramref name="tenantId"/> parameter and always asks the Gateway for the CALLER's own
/// branding (GET /user/me/branding) - same discipline as every other self-service endpoint here
/// (GetUserAsync, UpdateMyProfileAsync): the server derives "which Client" from the caller's own
/// validated JWT, never from a client-supplied id, so there is no path for one user to fetch
/// another Client's branding. The tenantId argument still matters at the TenantThemeService
/// level - passing null there skips calling this provider at all.
/// </summary>
public class ClientBrandingProvider(UserRepo users) : ITenantBrandingProvider
{
    public async Task<TenantBranding?> GetBrandingAsync(string tenantId, CancellationToken ct = default)
    {
        var branding = await users.GetMyBrandingAsync(ct);
        return branding is null
            ? null
            : new TenantBranding(
                TenantId: branding.clientId.ToString(),
                DisplayName: branding.displayName,
                LogoUrl: branding.logoUrl,
                PrimaryColor: branding.primaryColor);
    }
}
