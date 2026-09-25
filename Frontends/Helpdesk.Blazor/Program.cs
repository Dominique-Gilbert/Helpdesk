using Helpdesk.Blazor.Components;
using Helpdesk.Blazor.Repositories;
using Helpdesk.Blazor.Services;
using Helpdesk.Blazor.Theming;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MudBlazor.Services;
using TenantTheming;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMudServices();

// Cookie auth here, JWT everywhere behind the gateway - the browser only ever sees an
// httponly cookie, the JWT rides inside it as a custom claim (see CurrentUserTokenAccessor).
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
    });
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<CurrentUserTokenAccessor>();
builder.Services.AddScoped<UnauthorizedHandler>();
builder.Services.AddScoped<BaseRoleViewContext>();

// White-labeling - see TenantTheming (Shared/TenantTheming), a tenant-agnostic library with no
// reference to anything Helpdesk-specific, kept that way on purpose so it's a candidate to reuse
// as-is in another MudBlazor app (e.g. SupercardWeb) with nothing more than its own
// ITenantBrandingProvider implementation.
builder.Services.AddScoped<ITenantBrandingProvider, ClientBrandingProvider>();
builder.Services.AddScoped<TenantThemeService>();

// Resolves "gateway" to the Aspire resource of that name instead of a hardcoded host:port.
builder.Services.AddServiceDiscovery();
builder.Services.ConfigureHttpClientDefaults(http => http.AddServiceDiscovery());

// One typed HttpClient per repository, all pointed at the gateway. Each repo (except UserRepo,
// whose Login call happens before a token exists) attaches this circuit's JWT itself via
// CurrentUserTokenAccessor - not an AddHttpMessageHandler DelegatingHandler, because
// IHttpClientFactory builds those in its own internal DI scope, not the circuit's, and
// AuthenticationStateProvider throws if resolved outside a Razor component's scope.
builder.Services.AddHttpClient<TicketRepo>(client => client.BaseAddress = new Uri("http://gateway"));
builder.Services.AddHttpClient<AssetRepo>(client => client.BaseAddress = new Uri("http://gateway"));
builder.Services.AddHttpClient<AssignmentRepo>(client => client.BaseAddress = new Uri("http://gateway"));
builder.Services.AddHttpClient<SlaRepo>(client => client.BaseAddress = new Uri("http://gateway"));
builder.Services.AddHttpClient<UserRepo>(client => client.BaseAddress = new Uri("http://gateway"));
builder.Services.AddHttpClient<ClientRepo>(client => client.BaseAddress = new Uri("http://gateway"));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();
app.UseAntiforgery();

// GET, not POST: the AppBar's Logout menu item triggers this with a plain
// NavigationManager.NavigateTo(forceLoad: true), not a form submission.
app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.LocalRedirect("/login");
});

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapGet("/health/readiness", () => Results.Ok("ready"));

app.Run();
