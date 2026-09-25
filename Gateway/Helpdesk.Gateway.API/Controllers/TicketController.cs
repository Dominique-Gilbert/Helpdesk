using Helpdesk.Gateway.Dto;
using Helpdesk.Gateway.Interfacing.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Gateway.Api.Controllers;

/// <summary>
/// Controllers call interfacing and return. No mapping, no business rules, no gRPC types
/// leaking into the signature - if logic starts creeping in here it belongs in interfacing.
///
/// The one exception is GetCallerScope: extracting "who is asking" from the validated JWT is
/// exactly the ClaimsPrincipal-reading job a controller is for (interfacing stays framework-free
/// and testable without HttpContext) - same split UserController.CallerId already uses. The
/// actual visibility decision itself still lives in TicketService, not here.
/// </summary>
[ApiController]
[Route("[controller]")]
public class TicketController(ITicketService tickets, ITicketOverviewService overviews) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<DtoTicket>> Create([FromBody] CreateTicketDto request, CancellationToken ct)
    {
        var ticket = await tickets.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = ticket.id }, ticket);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DtoTicket>> Get(Guid id, CancellationToken ct)
    {
        var ticket = await tickets.GetAsync(id, this.GetCallerScope(), ct);
        return ticket is null ? NotFound() : Ok(ticket);
    }

    [HttpGet]
    public async Task<ActionResult<DtoTicketPage>> List(
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] Guid? clientId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default) =>
        Ok(await tickets.ListAsync(status, priority, page, pageSize, this.GetCallerScope(clientId), ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DtoTicket>> Update(Guid id, [FromBody] UpdateTicketDto request, CancellationToken ct)
    {
        var ticket = await tickets.UpdateAsync(id, request, this.GetCallerScope(), ct);
        return ticket is null ? NotFound() : Ok(ticket);
    }

    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult<DtoTicket>> Close(Guid id, CancellationToken ct)
    {
        var ticket = await tickets.CloseAsync(id, this.GetCallerScope(), ct);
        return ticket is null ? NotFound() : Ok(ticket);
    }

    [HttpPost("{id:guid}/comment")]
    public async Task<ActionResult<DtoTicket>> AddComment(Guid id, [FromBody] AddCommentDto request, CancellationToken ct)
    {
        var ticket = await tickets.AddCommentAsync(id, request, this.GetCallerScope(), ct);
        return ticket is null ? NotFound() : Ok(ticket);
    }

    /// <summary>Ticket, assignment and SLA clock in one object - the fan-out, visible.</summary>
    [HttpGet("{id:guid}/overview")]
    public async Task<ActionResult<DtoTicketOverview>> Overview(Guid id, CancellationToken ct)
    {
        var overview = await overviews.GetAsync(id, this.GetCallerScope(), ct);
        return overview is null ? NotFound() : Ok(overview);
    }
}
