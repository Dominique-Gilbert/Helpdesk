using Helpdesk.Gateway.Dto;
using Helpdesk.Gateway.Interfacing.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Gateway.Api.Controllers;

/// <summary>
/// Class-level, not per-method: unlike Assignment/SLA/User, nothing in the frontend reads
/// Assets except the Admin-only /assets page and its drawers (AssetDrawer, AssignAssetDrawer) -
/// there's no "any authenticated user can view" case here to carve out.
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize(Roles = "Admin,BaseRole")]
public class AssetController(IAssetService assets) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<DtoAsset>> Create([FromBody] CreateAssetDto request, CancellationToken ct)
    {
        var asset = await assets.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = asset.id }, asset);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DtoAsset>> Get(Guid id, CancellationToken ct)
    {
        // Guid.Empty specifically (not just "not found") throws deep inside Asset.API's own
        // ParseId rather than surfacing as a clean 404 - reject it here before it ever reaches
        // gRPC, same reasoning as every other {id:guid} route below.
        if (id == Guid.Empty) return BadRequest("id must not be empty.");

        var asset = await assets.GetAsync(id, this.GetTenantScope(), ct);
        return asset is null ? NotFound() : Ok(asset);
    }

    [HttpGet]
    public async Task<ActionResult<DtoAssetPage>> List(
        [FromQuery] string? status,
        [FromQuery] string? type,
        [FromQuery] Guid? clientId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default) =>
        Ok(await assets.ListAsync(status, type, page, pageSize, this.GetTenantScope(clientId), ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DtoAsset>> Update(Guid id, [FromBody] UpdateAssetDto request, CancellationToken ct)
    {
        if (id == Guid.Empty) return BadRequest("id must not be empty.");

        var asset = await assets.UpdateAsync(id, request, this.GetTenantScope(), ct);
        return asset is null ? NotFound() : Ok(asset);
    }

    [HttpPost("{id:guid}/assign")]
    public async Task<ActionResult<DtoAsset>> Assign(Guid id, [FromBody] AssignAssetDto request, CancellationToken ct)
    {
        if (id == Guid.Empty) return BadRequest("id must not be empty.");

        var asset = await assets.AssignAsync(id, request, this.GetTenantScope(), ct);
        return asset is null ? NotFound() : Ok(asset);
    }

    [HttpPost("{id:guid}/return")]
    public async Task<ActionResult<DtoAsset>> Return(Guid id, [FromBody] AssignAssetDto? request, CancellationToken ct)
    {
        if (id == Guid.Empty) return BadRequest("id must not be empty.");

        var asset = await assets.ReturnAsync(id, request?.notes, this.GetTenantScope(), ct);
        return asset is null ? NotFound() : Ok(asset);
    }
}
