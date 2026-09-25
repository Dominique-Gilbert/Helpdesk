using Helpdesk.Assignments.Api.Services;
using Helpdesk.Assignments.Infrastructure;
using Helpdesk.Contracts.Events;
using Helpdesk.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Assignments.Api.Consumers;

/// <summary>
/// A closed ticket is no longer active work - the technician's load has to come back down,
/// or "load" just counts up forever and every technician eventually reads as permanently full
/// even while sitting idle. Independent of Ticket.API by construction, same as every other
/// consumer here - it only knows the contract, not that Ticket.API exists.
///
/// Freeing a slot is also one of the triggers BacklogDrainService sweeps on - a queued ticket
/// that found no capacity anywhere waits in AssignmentBacklogEntry until something (this, or a
/// technician being created/updated) opens somewhere for it to go.
/// </summary>
public class AssignmentTicketClosedConsumer(
    AssignmentContext db,
    BacklogDrainService backlog,
    ILogger<AssignmentTicketClosedConsumer> logger) : IEventConsumer<TicketClosed>
{
    public async Task ConsumeAsync(TicketClosed message, CancellationToken ct)
    {
        var assignment = await db.TicketAssignments
            .Include(a => a.Technician)
            .FirstOrDefaultAsync(a => a.TicketId == message.TicketId, ct);

        if (assignment?.Technician is null)
        {
            // Never assigned (e.g. still sitting in the backlog) - nothing to release.
            return;
        }

        var technician = assignment.Technician;
        if (technician.ActiveAssignments > 0)
        {
            technician.ActiveAssignments -= 1;
            technician.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            logger.LogInformation("Released one load slot from {Technician} (ticket {Reference} closed): now {Active}/{Max}",
                technician.FullName, message.Reference, technician.ActiveAssignments, technician.MaxConcurrent);

            await backlog.DrainAsync(ct);
        }
    }
}
