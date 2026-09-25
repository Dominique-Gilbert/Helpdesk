using Helpdesk.Contracts.Events;
using Helpdesk.Mapping;
using Helpdesk.Messaging;
using Helpdesk.Sla.Domain.Model;
using Helpdesk.Sla.Domain.Sla;
using Helpdesk.Sla.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Sla.Api.Consumers;

/// <summary>
/// A ticket that comes back from its post-close grace window gets a fresh full SLA window,
/// not the remainder of the old one - see SlaClockCalculator.Restart. Independent of
/// Ticket.API by construction, same as every other consumer here.
/// </summary>
public class SlaTicketReopenedConsumer(
    SlaContext db,
    ILogger<SlaTicketReopenedConsumer> logger) : IEventConsumer<TicketReopened>
{
    public async Task ConsumeAsync(TicketReopened message, CancellationToken ct)
    {
        var clock = await db.SlaClocks.FirstOrDefaultAsync(c => c.TicketId == message.TicketId, ct);
        if (clock is null)
        {
            logger.LogWarning("Ticket.Reopened for ticket {TicketId} with no SLA clock - ignoring", message.TicketId);
            return;
        }

        var priority = ProtoConverters.ToEnum(message.Priority, clock.Priority);
        SlaClockCalculator.Restart(clock, priority, DateTime.UtcNow);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Restarted SLA clock for reopened ticket {Reference}, due {DueAt:u}",
            clock.TicketReference, clock.DueAtUtc);
    }
}
