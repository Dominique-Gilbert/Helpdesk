using Helpdesk.Mock.Pipeline;
using Helpdesk.Sla.Domain.Model;
using Helpdesk.Sla.Domain.Sla;
using Xunit;

namespace Helpdesk.Sla.Tests;

public class SlaClockCalculatorTests
{
    private static readonly DateTime Now = MockSlaClocks.Now;

    [Theory]
    [InlineData(TicketPriority.Critical, 2)]
    [InlineData(TicketPriority.High, 8)]
    [InlineData(TicketPriority.Normal, 24)]
    [InlineData(TicketPriority.Low, 72)]
    public void Targets_are_pinned_to_policy(TicketPriority priority, int expectedHours)
    {
        Assert.Equal(TimeSpan.FromHours(expectedHours), SlaClockCalculator.TargetFor(priority));
    }

    [Fact]
    public void Due_time_is_start_plus_target()
    {
        Assert.Equal(Now.AddHours(2), SlaClockCalculator.DueAt(Now, TicketPriority.Critical));
    }

    [Fact]
    public void A_clock_is_not_breached_one_second_before_it_is_due()
    {
        var clock = MockSlaClocks.Running(TicketPriority.Critical);
        Assert.False(SlaClockCalculator.IsBreached(clock, clock.DueAtUtc.AddSeconds(-1)));
    }

    [Fact]
    public void A_clock_is_breached_exactly_on_its_due_time()
    {
        var clock = MockSlaClocks.Running(TicketPriority.Critical);
        Assert.True(SlaClockCalculator.IsBreached(clock, clock.DueAtUtc));
    }

    [Fact]
    public void A_stopped_clock_never_breaches()
    {
        var clock = MockSlaClocks.Stopped(TicketPriority.Critical);
        Assert.False(SlaClockCalculator.IsBreached(clock, clock.DueAtUtc.AddDays(30)));
    }

    [Fact]
    public void A_stopped_clock_reports_no_time_remaining()
    {
        var clock = MockSlaClocks.Stopped();
        Assert.Equal(TimeSpan.Zero, SlaClockCalculator.Remaining(clock, Now));
    }

    [Fact]
    public void Remaining_goes_negative_once_overdue()
    {
        var clock = MockSlaClocks.Running(TicketPriority.High);
        var remaining = SlaClockCalculator.Remaining(clock, clock.DueAtUtc.AddHours(1));

        Assert.True(remaining < TimeSpan.Zero);
        Assert.Equal(TimeSpan.FromHours(-1), remaining);
    }

    [Fact]
    public void Escalation_is_zero_inside_target()
    {
        var clock = MockSlaClocks.Running(TicketPriority.Normal);
        Assert.Equal(0, SlaClockCalculator.EscalationLevel(clock, clock.DueAtUtc.AddMinutes(-1)));
    }

    [Fact]
    public void Escalation_is_one_the_moment_it_breaches()
    {
        var clock = MockSlaClocks.Running(TicketPriority.Normal);
        Assert.Equal(1, SlaClockCalculator.EscalationLevel(clock, clock.DueAtUtc));
    }

    [Fact]
    public void Escalation_climbs_one_level_per_further_target_period()
    {
        var clock = MockSlaClocks.Running(TicketPriority.Critical); // 2h target

        Assert.Equal(1, SlaClockCalculator.EscalationLevel(clock, clock.DueAtUtc.AddHours(1)));
        Assert.Equal(2, SlaClockCalculator.EscalationLevel(clock, clock.DueAtUtc.AddHours(2)));
        Assert.Equal(3, SlaClockCalculator.EscalationLevel(clock, clock.DueAtUtc.AddHours(4)));
    }

    [Fact]
    public void Start_produces_a_running_clock_wired_to_its_ticket()
    {
        var ticketId = Guid.NewGuid();
        var clock = SlaClockCalculator.Start(ticketId, "TCK-20260918-000001", TicketPriority.High, Now);

        Assert.Equal(ticketId, clock.TicketId);
        Assert.Equal(SlaClockStatus.Running, clock.Status);
        Assert.Equal(Now.AddHours(8), clock.DueAtUtc);
    }

    [Fact]
    public void Stop_marks_a_running_clock_stopped()
    {
        var clock = MockSlaClocks.Running(TicketPriority.Normal);
        var stoppedAt = Now.AddHours(1);

        SlaClockCalculator.Stop(clock, stoppedAt);

        Assert.Equal(SlaClockStatus.Stopped, clock.Status);
        Assert.Equal(stoppedAt, clock.StoppedAtUtc);
    }

    [Fact]
    public void Stop_is_a_no_op_on_a_clock_that_has_already_stopped()
    {
        var clock = MockSlaClocks.Stopped(TicketPriority.Normal);
        var originalStoppedAt = clock.StoppedAtUtc;

        SlaClockCalculator.Stop(clock, Now.AddDays(1));

        Assert.Equal(originalStoppedAt, clock.StoppedAtUtc);
    }

    [Fact]
    public void Stop_also_stops_a_clock_that_already_breached()
    {
        // A ticket closed after its clock breached (the common case for a genuinely late
        // ticket) must stop escalating too - "closed" is what freezes the clock, not "still on
        // time when it was closed". Regression test for a bug where Stop() only ever handled
        // Running, silently no-opping on Breached and leaving EscalationLevel climbing forever.
        var clock = MockSlaClocks.Breached(TicketPriority.Normal);
        var stoppedAt = Now.AddHours(1);

        SlaClockCalculator.Stop(clock, stoppedAt);

        Assert.Equal(SlaClockStatus.Stopped, clock.Status);
        Assert.Equal(stoppedAt, clock.StoppedAtUtc);
        Assert.Equal(0, SlaClockCalculator.EscalationLevel(clock, stoppedAt.AddDays(30)));
    }

    [Fact]
    public void Restart_gives_a_fresh_full_window_from_now_not_the_old_remainder()
    {
        var clock = MockSlaClocks.Stopped(TicketPriority.Critical);
        var reopenedAt = Now.AddDays(2);

        SlaClockCalculator.Restart(clock, TicketPriority.Critical, reopenedAt);

        Assert.Equal(SlaClockStatus.Running, clock.Status);
        Assert.Equal(reopenedAt, clock.StartedAtUtc);
        Assert.Equal(reopenedAt.AddHours(2), clock.DueAtUtc);
        Assert.Null(clock.StoppedAtUtc);
        Assert.Null(clock.BreachedAtUtc);
    }

    [Fact]
    public void Restart_can_change_priority_if_the_ticket_priority_changed()
    {
        var clock = MockSlaClocks.Stopped(TicketPriority.Low);

        SlaClockCalculator.Restart(clock, TicketPriority.Critical, Now);

        Assert.Equal(TicketPriority.Critical, clock.Priority);
        Assert.Equal(Now.AddHours(2), clock.DueAtUtc);
    }
}
