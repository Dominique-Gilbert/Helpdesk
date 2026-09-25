using Helpdesk.Gateway.Dto;

namespace Helpdesk.Blazor.Services;

/// <summary>
/// Turns an SLA clock's remaining time into a chip color that fades continuously from green
/// through orange to red as the deadline approaches, rather than jumping between a handful of
/// fixed states - "on target" should visibly start to worry someone before it actually
/// breaches. Own copy of the priority-target policy (SLA.API's SlaClockCalculator.TargetFor) -
/// this is display-only rounding, not the source of truth for when a clock actually breaches.
/// </summary>
public static class SlaChipStyle
{
    private static TimeSpan TargetFor(string priority) => priority switch
    {
        "Critical" => TimeSpan.FromHours(2),
        "High" => TimeSpan.FromHours(8),
        "Normal" => TimeSpan.FromHours(24),
        "Low" => TimeSpan.FromHours(72),
        _ => TimeSpan.FromHours(24)
    };

    /// <summary>Null when there's nothing to color yet (no clock has started) or the clock is no
    /// longer counting down (Stopped) - the caller falls back to a neutral chip in both cases.</summary>
    public static string? BackgroundFor(DtoSlaClock? clock)
    {
        if (clock is null || clock.status == "Stopped") return null;

        var target = TargetFor(clock.priority).TotalSeconds;
        if (target <= 0) return null;

        var fraction = Math.Clamp(clock.remainingSeconds / target, 0, 1);
        var (r, g, b) = Interpolate(fraction);
        return $"background-color: rgb({r},{g},{b}) !important; color: #fff !important;";
    }

    private static (int R, int G, int B) Interpolate(double fraction)
    {
        // Two segments so the midpoint (half the target left) reads as a clear amber, not a
        // washed-out green/red blend: green -> orange across the upper half, orange -> red
        // across the lower half.
        (int R, int G, int B) green = (46, 204, 64);
        (int R, int G, int B) orange = (255, 152, 0);
        (int R, int G, int B) red = (211, 47, 47);

        var (from, to, t) = fraction >= 0.5
            ? (green, orange, (1 - fraction) / 0.5)
            : (orange, red, (0.5 - fraction) / 0.5);

        return (
            Lerp(from.R, to.R, t),
            Lerp(from.G, to.G, t),
            Lerp(from.B, to.B, t));
    }

    private static int Lerp(int from, int to, double t) => (int)Math.Round(from + (to - from) * t);
}
