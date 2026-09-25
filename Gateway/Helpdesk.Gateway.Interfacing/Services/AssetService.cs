using AutoMapper;
using Helpdesk.Assets.Grpc;
using Helpdesk.Gateway.Dto;
using Helpdesk.Gateway.Interfacing.Interfaces;

namespace Helpdesk.Gateway.Interfacing.Services;

public class AssetService(AssetGrpc.AssetGrpcClient client, IMapper mapper) : IAssetService
{
    public async Task<DtoAsset> CreateAsync(CreateAssetDto request, CancellationToken ct = default)
    {
        var response = await client.CreateAssetAsync(new CreateAssetRequest
        {
            Tag = request.tag ?? string.Empty,
            Name = request.name ?? string.Empty,
            SerialNumber = request.serialNumber ?? string.Empty,
            Type = request.type ?? "Laptop",
            Location = request.location ?? string.Empty
        }, cancellationToken: ct);

        return Map(response);
    }

    public async Task<DtoAsset?> GetAsync(Guid id, TenantScope scope, CancellationToken ct = default)
    {
        var asset = await TicketService.NotFoundAsNull(async () =>
            Map(await client.GetAssetAsync(new AssetRequest { Id = id.ToString() }, cancellationToken: ct)));

        // Not found, not forbidden - an asset belonging to another Client shouldn't even confirm
        // it exists, same reasoning as TicketService.GetAsync.
        return asset is not null && CanSee(asset, scope) ? asset : null;
    }

    public async Task<DtoAssetPage> ListAsync(string? status, string? type, int page, int pageSize, TenantScope scope, CancellationToken ct = default)
    {
        var response = await client.ListAssetsAsync(new ListAssetsRequest
        {
            Status = status ?? string.Empty,
            Type = type ?? string.Empty,
            Page = page,
            PageSize = pageSize
        }, cancellationToken: ct);

        var items = response.Assets.Select(Map).ToList();

        // Same rule as TicketService.ListAsync: BaseRole sees every Client's, or one specific
        // Client's via ClientFilter (the profile-menu switcher); Admin only ever their own.
        items = scope.Role == "BaseRole"
            ? scope.ClientFilter is { } filter ? items.Where(a => a.clientId == filter).ToList() : items
            : items.Where(a => a.clientId == scope.ClientId).ToList();

        return new DtoAssetPage
        {
            items = items,
            total = items.Count,
            page = response.Page,
            pageSize = response.PageSize
        };
    }

    public async Task<DtoAsset?> UpdateAsync(Guid id, UpdateAssetDto request, TenantScope scope, CancellationToken ct = default)
    {
        if (await GetAsync(id, scope, ct) is null) return null;

        return await TicketService.NotFoundAsNull(async () => Map(await client.UpdateAssetAsync(new UpdateAssetRequest
        {
            Id = id.ToString(),
            Name = request.name ?? string.Empty,
            SerialNumber = request.serialNumber ?? string.Empty,
            Location = request.location ?? string.Empty,
            Type = request.type ?? string.Empty,
            Status = request.status ?? string.Empty
        }, cancellationToken: ct)));
    }

    public async Task<DtoAsset?> AssignAsync(Guid id, AssignAssetDto request, TenantScope scope, CancellationToken ct = default)
    {
        if (await GetAsync(id, scope, ct) is null) return null;

        return await TicketService.NotFoundAsNull(async () => Map(await client.AssignAssetAsync(new AssignAssetRequest
        {
            AssetId = id.ToString(),
            TicketId = request.ticketId?.ToString() ?? string.Empty,
            TechnicianId = request.technicianId?.ToString() ?? string.Empty,
            AssignedTo = request.assignedTo ?? string.Empty,
            Notes = request.notes ?? string.Empty
        }, cancellationToken: ct)));
    }

    public async Task<DtoAsset?> ReturnAsync(Guid id, string? notes, TenantScope scope, CancellationToken ct = default)
    {
        if (await GetAsync(id, scope, ct) is null) return null;

        return await TicketService.NotFoundAsNull(async () => Map(await client.ReturnAssetAsync(new ReturnAssetRequest
        {
            AssetId = id.ToString(),
            Notes = notes ?? string.Empty
        }, cancellationToken: ct)));
    }

    private static bool CanSee(DtoAsset asset, TenantScope scope) =>
        scope.Role == "BaseRole"
            ? scope.ClientFilter is not { } filter || asset.clientId == filter
            : asset.clientId == scope.ClientId;

    private DtoAsset Map(AssetResponse response)
    {
        var dto = mapper.Map<DtoAsset>(response);
        dto.assignmentRecords = response.AssignmentRecords.Select(mapper.Map<DtoAssignmentRecord>).ToList();
        return dto;
    }
}
