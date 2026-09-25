using System.Net;
using System.Net.Http.Json;
using Helpdesk.Blazor.Services;
using Helpdesk.Gateway.Dto;

namespace Helpdesk.Blazor.Repositories;

public class AssignmentRepo(HttpClient http, CurrentUserTokenAccessor tokenAccessor, UnauthorizedHandler unauthorized)
    : GatewayRepoBase(http, tokenAccessor, unauthorized)
{
    private const string Endpoint = "/assignment";

    public async Task<List<DtoTechnician>> ListTechniciansAsync(Guid? clientId = null, CancellationToken ct = default)
    {
        var url = $"{Endpoint}/technician";
        if (clientId is { } id) url += $"?clientId={id}";

        return await GetJsonAsync<List<DtoTechnician>>(url, ct) ?? [];
    }

    public async Task<DtoTechnician?> GetTechnicianAsync(Guid id, CancellationToken ct = default)
    {
        var response = await GetAsync($"{Endpoint}/technician/{id}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DtoTechnician>(cancellationToken: ct);
    }

    public async Task<List<DtoRoutingRule>> ListRoutingRulesAsync(Guid? clientId = null, CancellationToken ct = default)
    {
        var url = $"{Endpoint}/rule?onlyActive=false";
        if (clientId is { } id) url += $"&clientId={id}";

        return await GetJsonAsync<List<DtoRoutingRule>>(url, ct) ?? [];
    }

    public async Task<DtoRoutingRule?> CreateRoutingRuleAsync(CreateRoutingRuleDto request, CancellationToken ct = default)
    {
        var response = await PostAsJsonAsync($"{Endpoint}/rule", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DtoRoutingRule>(cancellationToken: ct);
    }

    public async Task<DtoRoutingRule?> UpdateRoutingRuleAsync(Guid id, UpdateRoutingRuleDto request, CancellationToken ct = default)
    {
        var response = await PutAsJsonAsync($"{Endpoint}/rule/{id}", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DtoRoutingRule>(cancellationToken: ct);
    }

    public async Task<List<DtoAssignment>> ListAssignmentsAsync(Guid? technicianId = null, Guid? clientId = null, CancellationToken ct = default)
    {
        var url = $"{Endpoint}?page=1&pageSize=50";
        if (technicianId.HasValue) url += $"&technicianId={technicianId}";
        if (clientId.HasValue) url += $"&clientId={clientId}";

        return await GetJsonAsync<List<DtoAssignment>>(url, ct) ?? [];
    }

    public async Task<List<DtoBacklogEntry>> ListBacklogAsync(CancellationToken ct = default) =>
        await GetJsonAsync<List<DtoBacklogEntry>>($"{Endpoint}/backlog", ct) ?? [];
}
