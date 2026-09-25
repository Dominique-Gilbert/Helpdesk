using MudBlazor;

namespace Helpdesk.Blazor.Theming;

/// <summary>One theme, no toggle - a red primary matching the company logo.</summary>
public static class HelpdeskTheme
{
    public static readonly MudTheme Theme = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#CC0000",
            AppbarBackground = "#000000",
            AppbarText = "#FFFFFF",
            DrawerBackground = "#000000",
            DrawerText = "#FFFFFF",
            DrawerIcon = "#FFFFFF"
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#E53935",
            AppbarBackground = "#000000",
            DrawerBackground = "#000000",
            DrawerText = "#FFFFFF",
            DrawerIcon = "#FFFFFF"
        }
    };
}
