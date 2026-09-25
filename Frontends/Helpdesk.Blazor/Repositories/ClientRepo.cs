using System.Net.Http.Json;
using Helpdesk.Blazor.Services;
using Helpdesk.Gateway.Dto;

namespace Helpdesk.Blazor.Repositories;

public class ClientRepo(HttpClient http, CurrentUserTokenAccessor tokenAccessor, UnauthorizedHandler unauthorized)
    : GatewayRepoBase(http, tokenAccessor, unauthorized)
{
    private const string Endpoint = "/client";

    public async Task<DtoClientPage> ListAsync(int page = 1, int pageSize = 50, CancellationToken ct = default) =>
        await GetJsonAsync<DtoClientPage>($"{Endpoint}?page={page}&pageSize={pageSize}", ct) ?? new DtoClientPage();

    public async Task<DtoClient?> CreateAsync(CreateClientDto request, CancellationToken ct = default)
    {
        var response = await PostAsJsonAsync(Endpoint, request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DtoClient>(cancellationToken: ct);
    }

    public async Task<DtoClient?> UpdateAsync(Guid id, UpdateClientDto request, CancellationToken ct = default)
    {
        var response = await PutAsJsonAsync($"{Endpoint}/{id}", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DtoClient>(cancellationToken: ct);
    }
}
