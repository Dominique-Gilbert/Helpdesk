using Helpdesk.Tickets.Domain.Model;
using Helpdesk.Tickets.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Tickets.Api.Services;

/// <summary>
/// A closed ticket stays visible for a 24h grace window in case someone still has something to
/// add - see TicketService.AddComment for the reopen half of this. Past that window with no
/// comment, it is archived into Completed: same shape as SLA.API's SlaBreachMonitor, a polling
/// scan rather than a scheduled callback per ticket. Fine locally, not a design to carry into
/// production - a real system would schedule this at close time instead of polling for it.
/// </summary>
public class TicketCompletionMonitor(
    IServiceScopeFactory scopeFactory,
    ILogger<TicketCompletionMonitor> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan GracePeriod = TimeSpan.FromHours(24);

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
                logger.LogError(ex, "Ticket completion scan failed; will retry on the next tick");
            }
        }
    }

    private async Task ScanAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketContext>();

        var cutoff = DateTime.UtcNow - GracePeriod;

        // Status == Closed, not just ClosedAtUtc having a value: a ticket that got a comment
        // during its grace window is already Reopened (ClosedAtUtc cleared), so it can never
        // be picked up here by accident.
        var expired = await db.Tickets
            .Where(t => t.Status == TicketStatus.Closed && t.ClosedAtUtc != null && t.ClosedAtUtc <= cutoff)
            .Take(200)
            .ToListAsync(ct);

        if (expired.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var ticket in expired)
        {
            ticket.Status = TicketStatus.Completed;
            ticket.UpdatedAtUtc = now;
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Marked {Count} ticket(s) Completed after a 24h close grace period with no follow-up comment",
            expired.Count);
    }
}
