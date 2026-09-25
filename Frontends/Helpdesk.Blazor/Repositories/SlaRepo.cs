using System.Net;
using System.Net.Http.Json;
using Helpdesk.Blazor.Services;
using Helpdesk.Gateway.Dto;

namespace Helpdesk.Blazor.Repositories;

public class SlaRepo(HttpClient http, CurrentUserTokenAccessor tokenAccessor, UnauthorizedHandler unauthorized)
    : GatewayRepoBase(http, tokenAccessor, unauthorized)
{
    private const string Endpoint = "/sla";

    public async Task<DtoSlaClock?> GetForTicketAsync(Guid ticketId, CancellationToken ct = default)
    {
        var response = await GetAsync($"{Endpoint}/ticket/{ticketId}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DtoSlaClock>(cancellationToken: ct);
    }

    public async Task<List<DtoSlaClock>> ListAsync(string? status = null, Guid? clientId = null, CancellationToken ct = default)
    {
        var url = $"{Endpoint}?page=1&pageSize=50";
        if (!string.IsNullOrWhiteSpace(status)) url += $"&status={Uri.EscapeDataString(status)}";
        if (clientId is { } id) url += $"&clientId={id}";

        return await GetJsonAsync<List<DtoSlaClock>>(url, ct) ?? [];
    }

    public async Task<List<DtoEscalation>> ListEscalationsAsync(CancellationToken ct = default) =>
        await GetJsonAsync<List<DtoEscalation>>($"{Endpoint}/escalation", ct) ?? [];

    public async Task<DtoSlaClock?> AdjustAsync(Guid ticketId, AdjustSlaClockDto request, CancellationToken ct = default)
    {
        var response = await PostAsJsonAsync($"{Endpoint}/ticket/{ticketId}/adjust", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DtoSlaClock>(cancellationToken: ct);
    }
}
