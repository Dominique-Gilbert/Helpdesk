using Helpdesk.Assignments.Infrastructure;
using Helpdesk.Contracts.Events;
using Helpdesk.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Assignments.Api.Consumers;

/// <summary>
/// The reverse of AssignmentTicketClosedConsumer: a comment during the 24h grace window brings
/// the ticket back to active work, so the technician's load slot it released on close has to
/// come back too - otherwise a reopen-heavy ticket could make a technician read as having spare
/// capacity while still actually working it.
/// </summary>
public class AssignmentTicketReopenedConsumer(
    AssignmentContext db,
    ILogger<AssignmentTicketReopenedConsumer> logger) : IEventConsumer<TicketReopened>
{
    public async Task ConsumeAsync(TicketReopened message, CancellationToken ct)
    {
        var assignment = await db.TicketAssignments
            .Include(a => a.Technician)
            .FirstOrDefaultAsync(a => a.TicketId == message.TicketId, ct);

        if (assignment?.Technician is null)
        {
            return;
        }

        var technician = assignment.Technician;
        technician.ActiveAssignments += 1;
        technician.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Restored one load slot to {Technician} (ticket {Reference} reopened): now {Active}/{Max}",
            technician.FullName, message.Reference, technician.ActiveAssignments, technician.MaxConcurrent);
    }
}
