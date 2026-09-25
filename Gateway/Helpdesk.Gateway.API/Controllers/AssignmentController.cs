using Helpdesk.Gateway.Dto;
using Helpdesk.Gateway.Interfacing.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Gateway.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AssignmentController(IAssignmentService assignments) : ControllerBase
{
    // Every real caller of this whole controller is an Admin-only page (Technicians, Routing
    // rules, Recent assignments, the Assets Assign drawer's technician dropdown, the Technician
    // workload drawer) - Tickets, the one genuinely shared feature, gets its technician/routing
    // data through TicketService's own in-process calls to IAssignmentService, not through this
    // HTTP surface, so gating these here doesn't touch that path.
    [HttpGet("technician")]
    [Authorize(Roles = "Admin,BaseRole")]
    public async Task<ActionResult<IReadOnlyList<DtoTechnician>>> ListTechnicians(
        [FromQuery] string? team,
        [FromQuery] Guid? clientId,
        [FromQuery] bool onlyWithCapacity = false,
        CancellationToken ct = default) =>
        Ok(await assignments.ListTechniciansAsync(team, onlyWithCapacity, this.GetTenantScope(clientId), ct));

    [HttpGet("technician/{id:guid}")]
    [Authorize(Roles = "Admin,BaseRole")]
    public async Task<ActionResult<DtoTechnician>> GetTechnician(Guid id, CancellationToken ct)
    {
        var technician = await assignments.GetTechnicianAsync(id, this.GetTenantScope(), ct);
        return technician is null ? NotFound() : Ok(technician);
    }

    /// <summary>
    /// Not meant to be called directly by the Blazor client - the Gateway's UserService calls
    /// this itself when provisioning a Technician-role User. Kept as a real endpoint (rather
    /// than internal-only) for the same reason every other admin action here is: testable and
    /// consistent with the rest of the REST surface.
    /// </summary>
    [HttpPost("technician")]
    [Authorize(Roles = "Admin,BaseRole")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<DtoTechnician>> CreateTechnician([FromBody] CreateTechnicianDto request, CancellationToken ct)
    {
        var technician = await assignments.CreateTechnicianAsync(request, ct);
        return CreatedAtAction(nameof(GetTechnician), new { id = technician.id }, technician);
    }

    [HttpPut("technician/{id:guid}")]
    [Authorize(Roles = "Admin,BaseRole")]
    public async Task<ActionResult<DtoTechnician>> UpdateTechnician(Guid id, [FromBody] UpdateTechnicianDto request, CancellationToken ct)
    {
        var technician = await assignments.UpdateTechnicianAsync(id, request, ct);
        return technician is null ? NotFound() : Ok(technician);
    }

    [HttpGet("rule")]
    [Authorize(Roles = "Admin,BaseRole")]
    public async Task<ActionResult<IReadOnlyList<DtoRoutingRule>>> ListRules(
        [FromQuery] bool onlyActive = true,
        [FromQuery] Guid? clientId = null,
        CancellationToken ct = default) =>
        Ok(await assignments.ListRoutingRulesAsync(onlyActive, this.GetTenantScope(clientId), ct));

    [HttpPost("rule")]
    [Authorize(Roles = "Admin,BaseRole")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<DtoRoutingRule>> CreateRule([FromBody] CreateRoutingRuleDto request, CancellationToken ct)
    {
        var rule = await assignments.CreateRoutingRuleAsync(request, this.GetTenantScope(), ct);
        return CreatedAtAction(nameof(ListRules), rule);
    }

    [HttpPut("rule/{id:guid}")]
    [Authorize(Roles = "Admin,BaseRole")]
    public async Task<ActionResult<DtoRoutingRule>> UpdateRule(Guid id, [FromBody] UpdateRoutingRuleDto request, CancellationToken ct)
    {
        var rule = await assignments.UpdateRoutingRuleAsync(id, request, this.GetTenantScope(), ct);
        return rule is null ? NotFound() : Ok(rule);
    }

    /// <summary>
    /// 404 here means "the consumer has not run yet", which is a legitimate state. Not called by
    /// the Blazor client directly - the shared Tickets/Completed Tickets overview panel gets the
    /// same data through TicketOverviewService's own in-process call to IAssignmentService.
    /// </summary>
    [HttpGet("ticket/{ticketId:guid}")]
    [Authorize(Roles = "Admin,BaseRole")]
    public async Task<ActionResult<DtoAssignment>> GetForTicket(Guid ticketId, CancellationToken ct)
    {
        var assignment = await assignments.GetForTicketAsync(ticketId, ct);
        return assignment is null ? NotFound() : Ok(assignment);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,BaseRole")]
    public async Task<ActionResult<IReadOnlyList<DtoAssignment>>> List(
        [FromQuery] Guid? technicianId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] Guid? clientId = null,
        CancellationToken ct = default) =>
        Ok(await assignments.ListAssignmentsAsync(technicianId, page, pageSize, this.GetTenantScope(clientId), ct));

    /// <summary>Admin-only visibility into tickets level routing couldn't place anywhere.</summary>
    [HttpGet("backlog")]
    [Authorize(Roles = "Admin,BaseRole")]
    public async Task<ActionResult<IReadOnlyList<DtoBacklogEntry>>> ListBacklog(
        [FromQuery] string? category,
        CancellationToken ct = default) =>
        Ok(await assignments.ListBacklogAsync(category, ct));
}
