using Helpdesk.Sla.Domain.Model;

namespace Helpdesk.Sla.Domain.Sla;

/// <summary>
/// All SLA arithmetic, kept pure so SLA.Tests can pin the maths without a database or a
/// real clock. Every method takes "now" as a parameter for exactly that reason - a class
/// that reads DateTime.UtcNow internally cannot be tested at a boundary.
/// </summary>
public static class SlaClockCalculator
{
    /// <summary>Resolution targets. Business policy, deliberately in one place.</summary>
    public static TimeSpan TargetFor(TicketPriority priority) => priority switch
    {
        TicketPriority.Critical => TimeSpan.FromHours(2),
        TicketPriority.High => TimeSpan.FromHours(8),
        TicketPriority.Normal => TimeSpan.FromHours(24),
        TicketPriority.Low => TimeSpan.FromHours(72),
        _ => TimeSpan.FromHours(24)
    };

    public static DateTime DueAt(DateTime startedAtUtc, TicketPriority priority) =>
        startedAtUtc + TargetFor(priority);

    public static bool IsBreached(SlaClock clock, DateTime nowUtc) =>
        clock.Status == SlaClockStatus.Running && nowUtc >= clock.DueAtUtc;

    /// <summary>Negative once overdue; zero for a clock that has already been stopped.</summary>
    public static TimeSpan Remaining(SlaClock clock, DateTime nowUtc) =>
        clock.Status == SlaClockStatus.Stopped ? TimeSpan.Zero : clock.DueAtUtc - nowUtc;

    /// <summary>
    /// 0 while inside target, 1 the moment it is breached, then +1 for every further full
    /// target period overdue. A clock that is stopped never escalates.
    /// </summary>
    public static int EscalationLevel(SlaClock clock, DateTime nowUtc)
    {
        if (clock.Status == SlaClockStatus.Stopped) return 0;
        if (nowUtc < clock.DueAtUtc) return 0;

        var target = TargetFor(clock.Priority);
        if (target <= TimeSpan.Zero) return 1;

        var overdue = nowUtc - clock.DueAtUtc;
        return 1 + (int)(overdue.Ticks / target.Ticks);
    }

    public static SlaClock Start(Guid ticketId, string reference, TicketPriority priority, DateTime startedAtUtc) =>
        new()
        {
            TicketId = ticketId,
            TicketReference = reference,
            Priority = priority,
            StartedAtUtc = startedAtUtc,
            DueAtUtc = DueAt(startedAtUtc, priority),
            Status = SlaClockStatus.Running,
            CreatedAtUtc = startedAtUtc
        };

    /// <summary>No-op on a clock that has already stopped - stopping is idempotent, since both
    /// the manual StopSlaClock rpc and the Ticket.Closed consumer can race to call this. Running
    /// and Breached both transition to Stopped: a ticket closed after its clock already breached
    /// (the common case for a genuinely late ticket) must stop escalating too, exactly like one
    /// closed before breaching - "closed" is what freezes the clock, not "still on time when it
    /// was closed".</summary>
    public static void Stop(SlaClock clock, DateTime nowUtc)
    {
        if (clock.Status == SlaClockStatus.Stopped) return;

        clock.Status = SlaClockStatus.Stopped;
        clock.StoppedAtUtc = nowUtc;
        clock.UpdatedAtUtc = nowUtc;
    }

    /// <summary>A reopened ticket gets a fresh full SLA window from the moment it reopens, not
    /// the leftover time on the old clock - the old due time and any breach are wiped, though
    /// prior Escalation rows are left in place as history of the first cycle.</summary>
    public static void Restart(SlaClock clock, TicketPriority priority, DateTime nowUtc)
    {
        clock.Priority = priority;
        clock.Status = SlaClockStatus.Running;
        clock.StartedAtUtc = nowUtc;
        clock.DueAtUtc = DueAt(nowUtc, priority);
        clock.StoppedAtUtc = null;
        clock.BreachedAtUtc = null;
        clock.UpdatedAtUtc = nowUtc;
    }

    /// <summary>
    /// Shifts the due time by a delta - positive extends, negative shrinks. Only meaningful on
    /// a clock that is still counting down, so Stopped is rejected (nothing to adjust; the
    /// ticket is closed). A positive delta that pushes the due time back into the future
    /// un-breaches a Breached clock, on purpose - "give them 4 more hours" is exactly the kind
    /// of override an admin reaches for after a clock has already gone red.
    /// </summary>
    public static void Adjust(SlaClock clock, TimeSpan delta, DateTime nowUtc)
    {
        if (clock.Status == SlaClockStatus.Stopped)
        {
            throw new InvalidOperationException("Cannot adjust a clock that has already stopped.");
        }

        clock.DueAtUtc += delta;
        clock.UpdatedAtUtc = nowUtc;

        if (clock.DueAtUtc > nowUtc)
        {
            clock.Status = SlaClockStatus.Running;
            clock.BreachedAtUtc = null;
        }
        else if (clock.Status == SlaClockStatus.Running)
        {
            clock.Status = SlaClockStatus.Breached;
            clock.BreachedAtUtc = nowUtc;
        }
    }
}
