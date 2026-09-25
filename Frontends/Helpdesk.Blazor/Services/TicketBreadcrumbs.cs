using MudBlazor;

namespace Helpdesk.Blazor.Services;

/// <summary>
/// Tickets and Completed Tickets cross-link via breadcrumbs, the same way Management's three
/// sub-pages do (see ManagementBreadcrumbs) - Completed Tickets has no nav entry of its own,
/// it is only ever reached from here.
///
/// Support never gets a Completed Tickets breadcrumb, not even scoped to their own (see
/// AdminOrTechnicianOnly, which also blocks the page itself from a typed URL) - callers pass
/// includeCompleted: false for Support so the ticketsLabel pill renders alone.
/// </summary>
public static class TicketBreadcrumbs
{
    private const string TicketsHref = "/tickets";
    private const string CompletedHref = "/tickets/completed";

    public static List<BreadcrumbItem> For(string currentHref, string ticketsLabel, bool includeCompleted)
    {
        var items = new List<BreadcrumbItem>
        {
            new(ticketsLabel, href: TicketsHref == currentHref ? null : TicketsHref, disabled: TicketsHref == currentHref)
        };

        if (includeCompleted)
        {
            items.Add(new BreadcrumbItem(
                "Completed Tickets",
                href: CompletedHref == currentHref ? null : CompletedHref,
                disabled: CompletedHref == currentHref));
        }

        return items;
    }
}
