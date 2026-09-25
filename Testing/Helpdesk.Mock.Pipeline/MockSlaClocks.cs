using Helpdesk.Sla.Domain.Model;
using Helpdesk.Sla.Domain.Sla;

namespace Helpdesk.Mock.Pipeline;

public static class MockSlaClocks
{
    /// <summary>A fixed "now" so tests never depend on when they run.</summary>
    public static readonly DateTime Now = new(2026, 9, 18, 9, 0, 0, DateTimeKind.Utc);

    public static SlaClock Running(TicketPriority priority = TicketPriority.Normal, DateTime? startedAt = null) =>
        SlaClockCalculator.Start(Guid.NewGuid(), "TCK-20260918-ABC123", priority, startedAt ?? Now);

    public static SlaClock Stopped(TicketPriority priority = TicketPriority.Normal)
    {
        var clock = Running(priority);
        clock.Status = SlaClockStatus.Stopped;
        clock.StoppedAtUtc = Now.AddMinutes(30);
        return clock;
    }

    public static SlaClock Breached(TicketPriority priority = TicketPriority.Normal)
    {
        var clock = Running(priority);
        clock.Status = SlaClockStatus.Breached;
        clock.BreachedAtUtc = clock.DueAtUtc;
        return clock;
    }
}
