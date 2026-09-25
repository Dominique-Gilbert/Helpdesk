using MudBlazor;

namespace Helpdesk.Blazor.Services;

/// <summary>
/// The three Management sub-pages (Technicians, Routing rules, Recent assignments) cross-link
/// to each other via breadcrumbs, the same way SuperCard's Entity/Community/Group Management
/// pages do: one list of (label, route) pairs here so the three pages can't drift out of sync
/// with each other. The current page's entry comes back disabled with no href - see
/// HelpdeskBreadcrumbs for how that renders as the highlighted "you are here" pill.
/// </summary>
public static class ManagementBreadcrumbs
{
    private static readonly (string Label, string Href)[] Pages =
    [
        ("Technicians", "/technicians"),
        ("Routing rules", "/technicians/routing-rules"),
        ("Recent assignments", "/technicians/recent-assignments")
    ];

    public static List<BreadcrumbItem> For(string currentHref) =>
        Pages
            .Select(page => new BreadcrumbItem(
                page.Label,
                href: page.Href == currentHref ? null : page.Href,
                disabled: page.Href == currentHref))
            .ToList();
}
