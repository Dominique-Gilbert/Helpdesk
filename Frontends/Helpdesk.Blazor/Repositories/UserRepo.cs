using System.Net.Http.Json;
using Helpdesk.Blazor.Services;
using Helpdesk.Gateway.Dto;

namespace Helpdesk.Blazor.Repositories;

public class UserRepo(HttpClient http, CurrentUserTokenAccessor tokenAccessor, UnauthorizedHandler unauthorized)
    : GatewayRepoBase(http, tokenAccessor, unauthorized)
{
    private const string Endpoint = "/user";

    /// <summary>
    /// Null means bad credentials - the gateway answers 401, not a thrown exception the UI needs
    /// to parse. Deliberately bypasses GatewayRepoBase's helpers: those route a 401 to
    /// UnauthorizedHandler (stale session -> force logout), but here a 401 just means "wrong
    /// password" - there's no session yet to drop, and there's no token to attach either.
    /// </summary>
    public async Task<DtoLoginResult?> LoginAsync(LoginRequestDto request, CancellationToken ct = default)
    {
        var response = await Http.PostAsJsonAsync($"{Endpoint}/login", request, ct);
        if (!response.IsSuccessStatusCode) return null;

        return await response.Content.ReadFromJsonAsync<DtoLoginResult>(cancellationToken: ct);
    }

    /// <summary>
    /// The progressive/identifier-first login screen's pre-auth lookup - always returns SOME
    /// DtoClientBranding (never null), empty when there's nothing to brand with, per
    /// GetLoginBrandingAsync's own non-enumeration discipline. Deliberately bypasses
    /// GatewayRepoBase's helpers, same reasoning as LoginAsync - there's no session/token yet for
    /// AttachTokenAsync to attach, and a failure here should just fall back to the default look,
    /// not force a redirect.
    /// </summary>
    public async Task<DtoClientBranding> GetLoginBrandingAsync(string username, CancellationToken ct = default)
    {
        try
        {
            var response = await Http.GetAsync($"{Endpoint}/login/branding?username={Uri.EscapeDataString(username)}", ct);
            if (!response.IsSuccessStatusCode) return new DtoClientBranding();

            return await response.Content.ReadFromJsonAsync<DtoClientBranding>(cancellationToken: ct) ?? new DtoClientBranding();
        }
        catch
        {
            // Best-effort only - a transient Gateway hiccup on the login screen should render the
            // plain, unbranded form, not break the page.
            return new DtoClientBranding();
        }
    }

    /// <summary>clientId is BaseRole's own "switch between clients" filter - ignored server-side
    /// for anyone else, whose own view is always scoped to their own Client regardless.</summary>
    public async Task<List<DtoUserSummary>> ListAsync(Guid? clientId = null, CancellationToken ct = default)
    {
        var url = clientId is { } id ? $"{Endpoint}?clientId={id}" : Endpoint;
        return await GetJsonAsync<List<DtoUserSummary>>(url, ct) ?? [];
    }

    public async Task<DtoUserSummary?> CreateAsync(CreateUserDto request, CancellationToken ct = default)
    {
        var response = await PostAsJsonAsync(Endpoint, request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DtoUserSummary>(cancellationToken: ct);
    }

    public async Task<DtoUserSummary?> UpdateAsync(Guid id, UpdateUserDto request, CancellationToken ct = default)
    {
        var response = await PutAsJsonAsync($"{Endpoint}/{id}", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DtoUserSummary>(cancellationToken: ct);
    }

    public async Task<DtoUserSummary?> GetMeAsync(CancellationToken ct = default) =>
        await GetJsonAsync<DtoUserSummary>($"{Endpoint}/me", ct);

    /// <summary>Null means "no Client, use the app's own default theme" (204) - not an error, see
    /// UserController.MyBranding. Bypasses GetJsonAsync/EnsureSuccessStatusCode - System.Text.Json
    /// throws trying to parse an empty 204 body as JSON.</summary>
    public async Task<DtoClientBranding?> GetMyBrandingAsync(CancellationToken ct = default)
    {
        var response = await GetAsync($"{Endpoint}/me/branding", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent) return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DtoClientBranding>(cancellationToken: ct);
    }

    /// <summary>The "App Theme" button's save - Admin-only, scoped to the caller's own Client,
    /// see UserController.UpdateMyBranding.</summary>
    public async Task<DtoClientBranding?> UpdateMyBrandingAsync(UpdateMyBrandingDto request, CancellationToken ct = default)
    {
        var response = await PutAsJsonAsync($"{Endpoint}/me/branding", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DtoClientBranding>(cancellationToken: ct);
    }

    public async Task<DtoUserSummary?> UpdateMeAsync(UpdateMyProfileDto request, CancellationToken ct = default)
    {
        var response = await PutAsJsonAsync($"{Endpoint}/me", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DtoUserSummary>(cancellationToken: ct);
    }

    /// <summary>Unlike the other calls here, a failure is expected UI (wrong current password)
    /// rather than exceptional - returns the gateway's own detail message instead of throwing,
    /// the same shape Login already uses null for "bad credentials" rather than an exception.</summary>
    public async Task<(bool success, string? error)> ChangePasswordAsync(ChangePasswordDto request, CancellationToken ct = default)
    {
        var response = await PostAsJsonAsync($"{Endpoint}/me/change-password", request, ct);
        if (response.IsSuccessStatusCode) return (true, null);

        var problem = await response.Content.ReadFromJsonAsync<GatewayProblem>(cancellationToken: ct);
        return (false, problem?.detail ?? "Failed to change password.");
    }

    private sealed class GatewayProblem
    {
        public string? detail { get; set; }
    }
}
