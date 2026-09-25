using Helpdesk.Contracts.Events;
using Helpdesk.Messaging;
using Helpdesk.Sla.Domain.Model;
using Helpdesk.Sla.Domain.Sla;
using Helpdesk.Sla.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Sla.Api.Consumers;

/// <summary>
/// Closing a ticket should not leave its SLA clock ticking towards a breach that no longer
/// means anything. Independent of Ticket.API by construction, same as every other consumer
/// here - it only knows the contract, not that Ticket.API exists.
/// </summary>
public class SlaTicketClosedConsumer(
    SlaContext db,
    ILogger<SlaTicketClosedConsumer> logger) : IEventConsumer<TicketClosed>
{
    public async Task ConsumeAsync(TicketClosed message, CancellationToken ct)
    {
        var clock = await db.SlaClocks.FirstOrDefaultAsync(c => c.TicketId == message.TicketId, ct);
        if (clock is null)
        {
            logger.LogWarning("Ticket.Closed for ticket {TicketId} with no SLA clock - ignoring", message.TicketId);
            return;
        }

        if (clock.Status == SlaClockStatus.Stopped)
        {
            return; // idempotent: redelivery, or the clock was already stopped
        }

        SlaClockCalculator.Stop(clock, DateTime.UtcNow);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Stopped SLA clock for closed ticket {Reference}", clock.TicketReference);
    }
}
