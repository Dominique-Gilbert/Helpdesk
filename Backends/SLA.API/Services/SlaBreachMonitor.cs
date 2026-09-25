using Helpdesk.Contracts.Events;
using Helpdesk.Messaging;
using Helpdesk.Sla.Domain.Model;
using Helpdesk.Sla.Domain.Sla;
using Helpdesk.Sla.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Sla.Api.Services;

/// <summary>
/// Closes the loop (the brief's stretch goal): a polling scan for expired clocks that
/// raises an Escalation and publishes SLA.Breached, which Ticket.API consumes.
///
/// Polling, not scheduling. Honest about what it is: fine for a local stack, not a design
/// to carry into production - a real system would schedule the breach at clock-start time.
/// </summary>
public class SlaBreachMonitor(
    IServiceScopeFactory scopeFactory,
    IEventPublisher publisher,
    ILogger<SlaBreachMonitor> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ScanAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                // A bad tick must not kill the monitor for the lifetime of the process.
                logger.LogError(ex, "SLA breach scan failed; will retry on the next tick");
            }
        }
    }

    private async Task ScanAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SlaContext>();

        var now = DateTime.UtcNow;

        var expired = await db.SlaClocks
            .Include(c => c.Escalations)
            .Where(c => c.Status == SlaClockStatus.Running && c.DueAtUtc <= now)
            .Take(100)
            .ToListAsync(ct);

        if (expired.Count == 0) return;

        foreach (var clock in expired)
        {
            clock.Status = SlaClockStatus.Breached;
            clock.BreachedAtUtc = now;
            clock.UpdatedAtUtc = now;

            var level = Math.Max(1, SlaClockCalculator.EscalationLevel(clock, now));
            if (clock.Escalations.All(e => e.Level != level))
            {
                // db.Escalations.Add(...), not clock.Escalations.Add(...) - see SlaService.
                // AdjustSlaClock for why: Escalation.Id is a client-generated Guid, so adding
                // only to the navigation collection gets this tracked as Modified instead of
                // Added, which UPDATEs a nonexistent row (0 rows affected) instead of INSERTing.
                db.Escalations.Add(new Escalation
                {
                    SlaClockId = clock.Id,
                    Level = level,
                    Notes = $"{clock.Priority} target of {SlaClockCalculator.TargetFor(clock.Priority).TotalHours:0.#}h exceeded.",
                    RaisedAtUtc = now
                });
            }
        }

        await db.SaveChangesAsync(ct);

        foreach (var clock in expired)
        {
            await publisher.PublishAsync(new SlaBreached
            {
                TicketId = clock.TicketId,
                SlaClockId = clock.Id,
                Priority = clock.Priority.ToString(),
                DueAtUtc = clock.DueAtUtc,
                BreachedAtUtc = now
            }, ct);

            logger.LogWarning("SLA breached for ticket {Reference}; published SLA.Breached", clock.TicketReference);
        }
    }
}
