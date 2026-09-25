using System.Net;
using System.Net.Http.Json;
using Helpdesk.Blazor.Services;
using Helpdesk.Gateway.Dto;

namespace Helpdesk.Blazor.Repositories;

/// <summary>
/// The Supercard repo pattern: a plain class over HttpClient, a fixed endpoint path, and
/// DTOs in and out. No gRPC, no proto, no knowledge that four services exist behind this.
/// </summary>
public class TicketRepo(HttpClient http, CurrentUserTokenAccessor tokenAccessor, UnauthorizedHandler unauthorized)
    : GatewayRepoBase(http, tokenAccessor, unauthorized)
{
    private const string Endpoint = "/ticket";

    public async Task<DtoTicketPage> ListAsync(string? status = null, Guid? clientId = null, int page = 1, int pageSize = 25, CancellationToken ct = default)
    {
        var url = $"{Endpoint}?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(status)) url += $"&status={Uri.EscapeDataString(status)}";
        if (clientId is { } id) url += $"&clientId={id}";

        return await GetJsonAsync<DtoTicketPage>(url, ct) ?? new DtoTicketPage();
    }

    public async Task<DtoTicket?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var response = await GetAsync($"{Endpoint}/{id}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DtoTicket>(cancellationToken: ct);
    }

    public async Task<DtoTicketOverview?> GetOverviewAsync(Guid id, CancellationToken ct = default)
    {
        var response = await GetAsync($"{Endpoint}/{id}/overview", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DtoTicketOverview>(cancellationToken: ct);
    }

    public async Task<DtoTicket?> CreateAsync(CreateTicketDto request, CancellationToken ct = default)
    {
        var response = await PostAsJsonAsync(Endpoint, request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DtoTicket>(cancellationToken: ct);
    }

    public async Task<DtoTicket?> AddCommentAsync(Guid id, AddCommentDto request, CancellationToken ct = default)
    {
        var response = await PostAsJsonAsync($"{Endpoint}/{id}/comment", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DtoTicket>(cancellationToken: ct);
    }

    public async Task<DtoTicket?> CloseAsync(Guid id, CancellationToken ct = default)
    {
        var response = await PostAsync($"{Endpoint}/{id}/close", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DtoTicket>(cancellationToken: ct);
    }
}
