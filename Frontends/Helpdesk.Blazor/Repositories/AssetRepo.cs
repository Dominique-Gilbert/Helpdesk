using System.Net.Http.Json;
using Helpdesk.Blazor.Services;
using Helpdesk.Gateway.Dto;

namespace Helpdesk.Blazor.Repositories;

public class AssetRepo(HttpClient http, CurrentUserTokenAccessor tokenAccessor, UnauthorizedHandler unauthorized)
    : GatewayRepoBase(http, tokenAccessor, unauthorized)
{
    private const string Endpoint = "/asset";

    public async Task<DtoAssetPage> ListAsync(string? status = null, Guid? clientId = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var url = $"{Endpoint}?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(status)) url += $"&status={Uri.EscapeDataString(status)}";
        if (clientId is { } id) url += $"&clientId={id}";

        return await GetJsonAsync<DtoAssetPage>(url, ct) ?? new DtoAssetPage();
    }

    public async Task<DtoAsset?> CreateAsync(CreateAssetDto request, CancellationToken ct = default)
    {
        var response = await PostAsJsonAsync(Endpoint, request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DtoAsset>(cancellationToken: ct);
    }

    public async Task<DtoAsset?> UpdateAsync(Guid id, UpdateAssetDto request, CancellationToken ct = default)
    {
        var response = await PutAsJsonAsync($"{Endpoint}/{id}", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DtoAsset>(cancellationToken: ct);
    }

    public async Task<DtoAsset?> AssignAsync(Guid id, AssignAssetDto request, CancellationToken ct = default)
    {
        var response = await PostAsJsonAsync($"{Endpoint}/{id}/assign", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DtoAsset>(cancellationToken: ct);
    }

    public async Task<DtoAsset?> ReturnAsync(Guid id, string? notes = null, CancellationToken ct = default)
    {
        var response = await PostAsJsonAsync($"{Endpoint}/{id}/return", new AssignAssetDto { notes = notes ?? string.Empty }, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DtoAsset>(cancellationToken: ct);
    }
}
