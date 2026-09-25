namespace Helpdesk.Blazor.Services;

/// <summary>Shared between TicketActionsDrawer's SLA tab and TechnicianWorkloadDrawer's
/// assigned-tickets tab - both show the same "HH:MM:SS remaining/overdue by" reading.</summary>
public static class SlaTimeFormat
{
    public static string FormatRemaining(long seconds) =>
        seconds >= 0
            ? $"{FormatSpan(TimeSpan.FromSeconds(seconds))} remaining"
            : $"overdue by {FormatSpan(TimeSpan.FromSeconds(-seconds))}";

    // TimeSpan's "hh" custom format specifier is hours-within-the-current-day (00-23), not
    // total hours - past 24h it silently drops the day count instead of showing it, so this
    // uses TotalHours (uncapped) rather than the "d\.hh\:mm\:ss" format.
    public static string FormatSpan(TimeSpan span) =>
        $"{(int)span.TotalHours:00}:{span.Minutes:00}:{span.Seconds:00}";
}
