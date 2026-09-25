using Helpdesk.Gateway.Dto;
using Helpdesk.Gateway.Interfacing.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Gateway.Api.Controllers;

/// <summary>
/// Class-level [Authorize(Roles = "BaseRole")] - unlike every other Admin-gated controller here
/// (which also accepts "Admin,BaseRole"), Clients is exclusive to BaseRole; Admin does not get
/// it. See NavMenu/BaseRoleOnly on the frontend for the same exclusivity.
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize(Roles = "BaseRole")]
public class ClientController(IClientService clients) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<DtoClient>> Create([FromBody] CreateClientDto request, CancellationToken ct)
    {
        var client = await clients.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = client.id }, client);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DtoClient>> Get(Guid id, CancellationToken ct)
    {
        var client = await clients.GetAsync(id, ct);
        return client is null ? NotFound() : Ok(client);
    }

    [HttpGet]
    public async Task<ActionResult<DtoClientPage>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default) =>
        Ok(await clients.ListAsync(page, pageSize, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DtoClient>> Update(Guid id, [FromBody] UpdateClientDto request, CancellationToken ct)
    {
        var client = await clients.UpdateAsync(id, request, ct);
        return client is null ? NotFound() : Ok(client);
    }
}
