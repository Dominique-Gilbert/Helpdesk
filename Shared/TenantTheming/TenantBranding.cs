namespace TenantTheming;

/// <summary>
/// The handful of things that make an app look distinctly "this tenant's app" rather than the
/// product's own default look - not a general theme (fonts, spacing, layout stay the host app's
/// own, defined by whatever base <see cref="MudBlazor.MudTheme"/> it passes to
/// <see cref="TenantThemeService.ApplyAsync"/>). Every color is an optional hex string
/// (e.g. "#CC0000") - null means "keep whatever the base theme already uses there".
/// </summary>
public sealed record TenantBranding(
    string TenantId,
    string DisplayName,
    string? LogoUrl = null,
    string? PrimaryColor = null,
    string? SecondaryColor = null,
    string? AppBarBackgroundColor = null,
    string? AppBarTextColor = null);
