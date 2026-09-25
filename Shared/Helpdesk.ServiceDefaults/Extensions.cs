using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Helpdesk.ServiceDefaults;

/// <summary>
/// The SuperCard.ServiceDefaults equivalent: the cross-cutting host wiring every backend
/// and the gateway share, so none of them hand-rolls its own health endpoints.
/// </summary>
public static class Extensions
{
    public const string LiveTag = "live";
    public const string ReadyTag = "ready";

    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        // Every Backends:* / Kestrel "Grpc" endpoint in this stack is plaintext HTTP/2 (h2c) -
        // Grpc.Net.Client refuses to negotiate HTTP/2 over an http:// address unless this is set.
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        builder.Services
            .AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("process is up"), tags: [LiveTag]);

        // Needed by GrpcAuthForwarding.AddBearerForwarding to read the inbound Authorization
        // header off the current request when forwarding it to an outgoing gRPC call.
        builder.Services.AddHttpContextAccessor();

        // Lets every AddHttpClient/AddGrpcClient in this app (and in referenced class libraries,
        // e.g. Helpdesk.Gateway.Interfacing) resolve peers by Aspire resource name instead of a
        // hardcoded host:port - see Uri("http://_grpc.ticket-api") call sites.
        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http => http.AddServiceDiscovery());

        builder.Services.Configure<HostOptions>(options =>
        {
            options.ShutdownTimeout = TimeSpan.FromSeconds(15);
            // A consumer mid-message should not be killed because a sibling service is slow.
            options.ServicesStartConcurrently = true;
            options.ServicesStopConcurrently = true;
        });

        return builder;
    }

    /// <summary>
    /// readiness = "my dependencies answer" (DB checks opt in with the ready tag).
    /// liveness  = "the process has not wedged".
    /// Both must be served over HTTP/1.1, which is why every service exposes a second,
    /// non-gRPC endpoint - see appsettings.json.
    /// </summary>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // The JWT fallback policy (see JwtAuthExtensions.AddHelpdeskJwtAuth) requires an
        // authenticated caller on every endpoint by default - health checks are Aspire/Compose
        // infrastructure, not application data, so they opt back out explicitly.
        app.MapHealthChecks("/health").AllowAnonymous();
        app.MapHealthChecks("/health/readiness", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(ReadyTag)
        }).AllowAnonymous();
        app.MapHealthChecks("/health/liveness", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(LiveTag)
        }).AllowAnonymous();
        return app;
    }
}
