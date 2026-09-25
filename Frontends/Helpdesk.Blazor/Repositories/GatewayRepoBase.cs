using System.Net.Http.Headers;
using System.Net.Http.Json;
using Helpdesk.Blazor.Services;

namespace Helpdesk.Blazor.Repositories;

/// <summary>
/// Shared plumbing for every gateway-backed repo: attaches this circuit's JWT before each call
/// (not via an AddHttpMessageHandler DelegatingHandler - IHttpClientFactory builds those in its
/// own internal DI scope, not the circuit's, and AuthenticationStateProvider throws if resolved
/// outside a Razor component's scope; these repos ARE resolved for the circuit, so it's safe
/// here), and routes every response through UnauthorizedHandler so a 401 signs the circuit out
/// instead of surfacing as an inline error.
/// </summary>
public abstract class GatewayRepoBase(HttpClient http, CurrentUserTokenAccessor tokenAccessor, UnauthorizedHandler unauthorized)
{
    /// <summary>Exposes the base's captured HttpClient to derived types that need it directly
    /// (e.g. UserRepo.LoginAsync bypassing the token/401 helpers below) without those types
    /// capturing the primary-constructor parameter themselves, which triggers CS9107.</summary>
    protected HttpClient Http => http;

    private async Task AttachTokenAsync()
    {
        var token = await tokenAccessor.GetTokenAsync();
        http.DefaultRequestHeaders.Authorization =
            string.IsNullOrEmpty(token) ? null : new AuthenticationHeaderValue("Bearer", token);
    }

    protected async Task<HttpResponseMessage> GetAsync(string url, CancellationToken ct)
    {
        await AttachTokenAsync();
        var response = await http.GetAsync(url, ct);
        unauthorized.RedirectIfUnauthorized(response);
        return response;
    }

    protected async Task<T?> GetJsonAsync<T>(string url, CancellationToken ct)
    {
        var response = await GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
    }

    protected async Task<HttpResponseMessage> PostAsJsonAsync<TReq>(string url, TReq body, CancellationToken ct)
    {
        await AttachTokenAsync();
        var response = await http.PostAsJsonAsync(url, body, ct);
        unauthorized.RedirectIfUnauthorized(response);
        return response;
    }

    protected async Task<HttpResponseMessage> PostAsync(string url, CancellationToken ct)
    {
        await AttachTokenAsync();
        var response = await http.PostAsync(url, content: null, ct);
        unauthorized.RedirectIfUnauthorized(response);
        return response;
    }

    protected async Task<HttpResponseMessage> PutAsJsonAsync<TReq>(string url, TReq body, CancellationToken ct)
    {
        await AttachTokenAsync();
        var response = await http.PutAsJsonAsync(url, body, ct);
        unauthorized.RedirectIfUnauthorized(response);
        return response;
    }
}
