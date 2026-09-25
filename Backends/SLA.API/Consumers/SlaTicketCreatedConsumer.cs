using Helpdesk.Contracts.Events;
using Helpdesk.Mapping;
using Helpdesk.Messaging;
using Helpdesk.Sla.Domain.Model;
using Helpdesk.Sla.Domain.Sla;
using Helpdesk.Sla.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Sla.Api.Consumers;

/// <summary>
/// The other half of the fan-out: start an SLA clock for the new ticket.
///
/// Independent of Assignment.API by construction - different Pulsar subscription, different
/// database, no call between them. If Assignment.API is stopped, this still runs; that is the
/// test.
/// </summary>
public class SlaTicketCreatedConsumer(
    SlaContext db,
    ILogger<SlaTicketCreatedConsumer> logger) : IEventConsumer<TicketCreated>
{
    public async Task ConsumeAsync(TicketCreated message, CancellationToken ct)
    {
        if (await db.SlaClocks.AnyAsync(c => c.TicketId == message.TicketId, ct))
        {
            logger.LogInformation("SLA clock already running for ticket {Reference} - skipping", message.Reference);
            return;
        }

        var priority = ProtoConverters.ToEnum(message.Priority, TicketPriority.Normal);
        var startedAt = message.CreatedAtUtc == default ? DateTime.UtcNow : message.CreatedAtUtc;

        var clock = SlaClockCalculator.Start(message.TicketId, message.Reference, priority, startedAt);
        clock.ClientId = message.ClientId;

        db.SlaClocks.Add(clock);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Started {Priority} SLA clock for ticket {Reference}, due {DueAt:u}",
            priority, message.Reference, clock.DueAtUtc);
    }
}
