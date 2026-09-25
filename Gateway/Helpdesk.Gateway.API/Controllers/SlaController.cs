using Helpdesk.Gateway.Dto;
using Helpdesk.Gateway.Interfacing.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Gateway.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class SlaController(ISlaService slas) : ControllerBase
{
    // NOT Admin-gated, unlike ListEscalations/Stop below: the shared Tickets page (every role)
    // fetches SLA chips for its own grid via List, and TicketActionsDrawer's Overview via
    // GetForTicket - ISlaService itself does the real scoping (Admin sees everything, everyone
    // else only clocks for tickets ITicketService already says are theirs), so this is safe to
    // leave open to any authenticated caller rather than break those.
    [HttpGet("ticket/{ticketId:guid}")]
    public async Task<ActionResult<DtoSlaClock>> GetForTicket(Guid ticketId, CancellationToken ct)
    {
        var clock = await slas.GetForTicketAsync(ticketId, this.GetCallerScope(), ct);
        return clock is null ? NotFound() : Ok(clock);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DtoSlaClock>>> List(
        [FromQuery] string? status,
        [FromQuery] Guid? clientId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default) =>
        Ok(await slas.ListAsync(status, page, pageSize, this.GetCallerScope(clientId), ct));

    [HttpGet("escalation")]
    [Authorize(Roles = "Admin,BaseRole")]
    public async Task<ActionResult<IReadOnlyList<DtoEscalation>>> ListEscalations(
        [FromQuery] Guid? slaClockId,
        CancellationToken ct = default) =>
        Ok(await slas.ListEscalationsAsync(slaClockId, ct));

    [HttpPost("ticket/{ticketId:guid}/stop")]
    [Authorize(Roles = "Admin,BaseRole")]
    public async Task<ActionResult<DtoSlaClock>> Stop(Guid ticketId, CancellationToken ct)
    {
        var clock = await slas.StopAsync(ticketId, ct);
        return clock is null ? NotFound() : Ok(clock);
    }

    [HttpPost("ticket/{ticketId:guid}/adjust")]
    [Authorize(Roles = "Admin,BaseRole")]
    public async Task<ActionResult<DtoSlaClock>> Adjust(Guid ticketId, [FromBody] AdjustSlaClockDto request, CancellationToken ct)
    {
        var adjustedBy = User.Identity?.Name ?? "unknown";
        var clock = await slas.AdjustAsync(ticketId, request, adjustedBy, ct);
        return clock is null ? NotFound() : Ok(clock);
    }
}
