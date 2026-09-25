using MudBlazor;

namespace TenantTheming;

/// <summary>
/// Scoped per Blazor circuit (one per logged-in user's session), the same lifetime as the rest
/// of a typical auth-aware app. A layout component calls <see cref="ApplyAsync"/> once it knows
/// which tenant (if any) the current user belongs to, binds its <c>MudThemeProvider</c> to
/// <see cref="Theme"/>, and re-renders on <see cref="OnChange"/>.
/// </summary>
public class TenantThemeService(ITenantBrandingProvider provider)
{
    private MudTheme _baseTheme = new();

    public MudTheme Theme { get; private set; } = new();
    public TenantBranding? Branding { get; private set; }

    public event Action? OnChange;

    /// <summary>
    /// <paramref name="baseTheme"/> is the host app's own theme (typography, layout, its default
    /// palette) - this never touches it, only builds a new <see cref="MudTheme"/> layered on top
    /// of it. <paramref name="tenantId"/> null means "no tenant" (e.g. an internal/admin account
    /// that belongs to no single tenant) - resets to the base theme with no branding applied.
    /// </summary>
    public async Task ApplyAsync(MudTheme baseTheme, string? tenantId, CancellationToken ct = default)
    {
        _baseTheme = baseTheme;
        Branding = string.IsNullOrWhiteSpace(tenantId) ? null : await provider.GetBrandingAsync(tenantId, ct);
        Theme = Branding is null ? baseTheme : Build(baseTheme, Branding);
        OnChange?.Invoke();
    }

    /// <summary>Re-runs the last ApplyAsync's branding lookup and rebuilds the theme - for a
    /// BaseRole-managed field (a client's color/logo) changing while someone from that tenant is
    /// already mid-session, without them having to log out and back in.</summary>
    public Task RefreshAsync(CancellationToken ct = default) =>
        ApplyAsync(_baseTheme, Branding?.TenantId, ct);

    /// <summary>Public and static on purpose - a pre-auth page (no circuit, no DI-resolved
    /// ITenantBrandingProvider/scoped service lifetime to lean on yet, e.g. a static
    /// server-rendered login screen) can still build the exact same themed MudTheme this service
    /// uses internally, from whatever branding it already fetched some other way.</summary>
    public static MudTheme Build(MudTheme baseTheme, TenantBranding branding) => new()
    {
        Typography = baseTheme.Typography,
        LayoutProperties = baseTheme.LayoutProperties,
        Shadows = baseTheme.Shadows,
        ZIndex = baseTheme.ZIndex,
        PaletteLight = BuildPalette(baseTheme.PaletteLight, branding),
        PaletteDark = BuildPalette(baseTheme.PaletteDark, branding) as PaletteDark ?? baseTheme.PaletteDark
    };

    // Only the handful of surfaces TenantBranding actually carries are overridden - everything
    // else on the palette (success/warning/error colors, text shades, etc.) stays whatever the
    // host app's own base theme already set.
    private static Palette BuildPalette(Palette basePalette, TenantBranding branding)
    {
        Palette palette = basePalette is PaletteDark ? new PaletteDark() : new PaletteLight();

        CopyFrom(basePalette, palette);

        if (branding.PrimaryColor is { } primary) palette.Primary = primary;
        if (branding.SecondaryColor is { } secondary) palette.Secondary = secondary;
        if (branding.AppBarBackgroundColor is { } appBarBg) palette.AppbarBackground = appBarBg;
        if (branding.AppBarTextColor is { } appBarText) palette.AppbarText = appBarText;

        return palette;
    }

    // MudBlazor's Palette has no built-in clone, and copying every property by hand would be
    // fragile across MudBlazor versions - the properties that matter for a rebrand are the ones
    // explicitly copied here; anything else falls back to whatever a fresh Palette() defaults to,
    // same as the base theme not specifying it either.
    private static void CopyFrom(Palette from, Palette to)
    {
        to.Primary = from.Primary;
        to.Secondary = from.Secondary;
        to.Tertiary = from.Tertiary;
        to.AppbarBackground = from.AppbarBackground;
        to.AppbarText = from.AppbarText;
        to.DrawerBackground = from.DrawerBackground;
        to.DrawerText = from.DrawerText;
        to.DrawerIcon = from.DrawerIcon;
        to.Background = from.Background;
        to.Surface = from.Surface;
        to.TextPrimary = from.TextPrimary;
        to.TextSecondary = from.TextSecondary;
    }
}
