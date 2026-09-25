using Helpdesk.Contracts.Events;
using Helpdesk.Messaging;
using Helpdesk.Tickets.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Tickets.Api.Consumers;

/// <summary>
/// The stretch goal, wired: SLA.API notices a clock has expired and says so; this service
/// flags the ticket. Ticket.API has no idea SLA.API exists - it only knows the contract.
///
/// Named TicketSlaBreachedConsumer rather than SlaBreachedConsumer so its Pulsar subscription
/// name stays unique across the whole topic.
/// </summary>
public class TicketSlaBreachedConsumer(TicketContext db, ILogger<TicketSlaBreachedConsumer> logger)
    : IEventConsumer<SlaBreached>
{
    public async Task ConsumeAsync(SlaBreached message, CancellationToken ct)
    {
        var ticket = await db.Tickets
            .FirstOrDefaultAsync(t => t.Id == message.TicketId, ct);

        if (ticket is null)
        {
            logger.LogWarning("SLA.Breached for unknown ticket {TicketId} - ignoring", message.TicketId);
            return;
        }

        if (ticket.SlaBreached) return; // idempotent: redelivery must not churn the row

        ticket.SlaBreached = true;
        ticket.SlaBreachedAtUtc = message.BreachedAtUtc;
        ticket.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Flagged ticket {Reference} as SLA-breached", ticket.Reference);
    }
}
