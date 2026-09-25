using Helpdesk.Gateway.Dto;
using Helpdesk.Gateway.Interfacing.Interfaces;

namespace Helpdesk.Gateway.Interfacing.Services;

/// <summary>
/// One interfacing method, three backends. The two side-calls run concurrently because
/// neither depends on the other - the same independence the event pipeline relies on,
/// expressed on the read path.
/// </summary>
public class TicketOverviewService(
    ITicketService tickets,
    IAssignmentService assignments,
    ISlaService slas) : ITicketOverviewService
{
    public async Task<DtoTicketOverview?> GetAsync(Guid ticketId, TicketAccessScope scope, CancellationToken ct = default)
    {
        var ticket = await tickets.GetAsync(ticketId, scope, ct);
        if (ticket is null) return null;

        var assignmentTask = assignments.GetForTicketAsync(ticketId, ct);
        var slaTask = slas.GetForTicketAsync(ticketId, scope, ct);

        await Task.WhenAll(assignmentTask, slaTask);

        return new DtoTicketOverview
        {
            ticket = ticket,
            assignment = assignmentTask.Result,
            slaClock = slaTask.Result
        };
    }
}
