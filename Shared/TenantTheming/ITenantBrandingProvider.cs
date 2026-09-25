namespace TenantTheming;

/// <summary>
/// The one seam a host app implements to plug its own tenant/branding source into this library -
/// a Gateway HTTP call, a gRPC call, a local cache, whatever. This library never assumes how
/// branding is stored or fetched, only that it can be asked for by tenant id.
/// </summary>
public interface ITenantBrandingProvider
{
    /// <summary>Null means "no branding for this tenant" (e.g. the tenant doesn't exist, or the
    /// current user belongs to no tenant at all) - the caller should fall back to the host app's
    /// own default theme, not treat it as an error.</summary>
    Task<TenantBranding?> GetBrandingAsync(string tenantId, CancellationToken ct = default);
}
