using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

var builder = DistributedApplication.CreateBuilder(args);

var appHostSettings = builder.Configuration
    .GetSection("AppHost")
    .Get<AppHostSettings>() ?? new AppHostSettings();

// SQL Server is the developer's own local instance, never a container - value comes from
// this AppHost's own appsettings.Development.json ("ConnectionStrings:ticketdb" etc.), same
// pattern SuperCard.AppHost uses for its SQL Server databases.
var ticketDb = builder.AddConnectionString("ticketdb");
var assetDb = builder.AddConnectionString("assetdb");
var assignmentDb = builder.AddConnectionString("assignmentdb");
var slaDb = builder.AddConnectionString("sladb");
var userDb = builder.AddConnectionString("userdb");
var clientDb = builder.AddConnectionString("clientdb");

// One shared signing key/issuer/audience for every host that issues or validates a JWT
// (User.API issues, the gateway and every backend validate independently). Fixed dev values -
// not meant to survive past local development.
var jwtSigningKey = builder.AddParameter("jwt-signing-key", "helpdesk-dev-signing-key-change-me-32chars!", secret: true);
var jwtIssuer = builder.AddParameter("jwt-issuer", "helpdesk");
var jwtAudience = builder.AddParameter("jwt-audience", "helpdesk");

var pulsar = PulsarFactory.PreparePulsar(builder);


var ticketApi = builder.AddProject<Projects.Ticket_API>("ticket-api", launchProfileName: null)
    .WithEndpoint(name: "Http", scheme: "http", port: appHostSettings.TicketHttpPort, targetPort: appHostSettings.TicketHttpPort, isProxied: false)
    .WithEndpoint(name: "Grpc", scheme: "http", port: appHostSettings.TicketGrpcPort, targetPort: appHostSettings.TicketGrpcPort, isProxied: false)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("Kestrel__Endpoints__Http__Url", $"http://0.0.0.0:{appHostSettings.TicketHttpPort.ToString()}")
    .WithEnvironment("Kestrel__Endpoints__Http__Protocols", "Http1")
    .WithEnvironment("Kestrel__Endpoints__Grpc__Url", $"http://0.0.0.0:{appHostSettings.TicketGrpcPort.ToString()}")
    .WithEnvironment("Kestrel__Endpoints__Grpc__Protocols", "Http2")
    .WithEnvironment("ConnectionStrings__DefaultConnection", ticketDb.Resource.ConnectionStringExpression)
    .WithReference(ticketDb)
    .WithEnvironment("Jwt__SigningKey", jwtSigningKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
    .WithEnvironment("PULSAR_SERVICE_URL", "pulsar://localhost:6650")
    .WaitFor(pulsar.Broker);

var assetApi = builder.AddProject<Projects.Asset_API>("asset-api", launchProfileName: null)
    .WithEndpoint(name: "Http", scheme: "http", port: appHostSettings.AssetHttpPort, targetPort: appHostSettings.AssetHttpPort, isProxied: false)
    .WithEndpoint(name: "Grpc", scheme: "http", port: appHostSettings.AssetGrpcPort, targetPort: appHostSettings.AssetGrpcPort, isProxied: false)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("Kestrel__Endpoints__Http__Url", $"http://0.0.0.0:{appHostSettings.AssetHttpPort.ToString()}")
    .WithEnvironment("Kestrel__Endpoints__Http__Protocols", "Http1")
    .WithEnvironment("Kestrel__Endpoints__Grpc__Url", $"http://0.0.0.0:{appHostSettings.AssetGrpcPort.ToString()}")
    .WithEnvironment("Kestrel__Endpoints__Grpc__Protocols", "Http2")
    .WithEnvironment("ConnectionStrings__DefaultConnection", assetDb.Resource.ConnectionStringExpression)
    .WithReference(assetDb)
    .WithEnvironment("Jwt__SigningKey", jwtSigningKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience);

var assignmentApi = builder.AddProject<Projects.Assignment_API>("assignment-api", launchProfileName: null)
    .WithEndpoint(name: "Http", scheme: "http", port: appHostSettings.AssignmentHttpPort, targetPort: appHostSettings.AssignmentHttpPort, isProxied: false)
    .WithEndpoint(name: "Grpc", scheme: "http", port: appHostSettings.AssignmentGrpcPort, targetPort: appHostSettings.AssignmentGrpcPort, isProxied: false)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("Kestrel__Endpoints__Http__Url", $"http://0.0.0.0:{appHostSettings.AssignmentHttpPort.ToString()}")
    .WithEnvironment("Kestrel__Endpoints__Http__Protocols", "Http1")
    .WithEnvironment("Kestrel__Endpoints__Grpc__Url", $"http://0.0.0.0:{appHostSettings.AssignmentGrpcPort.ToString()}")
    .WithEnvironment("Kestrel__Endpoints__Grpc__Protocols", "Http2")
    .WithEnvironment("ConnectionStrings__DefaultConnection", assignmentDb.Resource.ConnectionStringExpression)
    .WithReference(assignmentDb)
    .WithEnvironment("Jwt__SigningKey", jwtSigningKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
    .WithEnvironment("PULSAR_SERVICE_URL", "pulsar://localhost:6650")
    .WaitFor(pulsar.Broker);

var slaApi = builder.AddProject<Projects.SLA_API>("sla-api", launchProfileName: null)
    .WithEndpoint(name: "Http", scheme: "http", port: appHostSettings.SlaHttpPort, targetPort: appHostSettings.SlaHttpPort, isProxied: false)
    .WithEndpoint(name: "Grpc", scheme: "http", port: appHostSettings.SlaGrpcPort, targetPort: appHostSettings.SlaGrpcPort, isProxied: false)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("Kestrel__Endpoints__Http__Url", $"http://0.0.0.0:{appHostSettings.SlaHttpPort.ToString()}")
    .WithEnvironment("Kestrel__Endpoints__Http__Protocols", "Http1")
    .WithEnvironment("Kestrel__Endpoints__Grpc__Url", $"http://0.0.0.0:{appHostSettings.SlaGrpcPort.ToString()}")
    .WithEnvironment("Kestrel__Endpoints__Grpc__Protocols", "Http2")
    .WithEnvironment("ConnectionStrings__DefaultConnection", slaDb.Resource.ConnectionStringExpression)
    .WithReference(slaDb)
    .WithEnvironment("Jwt__SigningKey", jwtSigningKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
    .WithEnvironment("PULSAR_SERVICE_URL", "pulsar://localhost:6650")
    .WaitFor(pulsar.Broker);

var userApi = builder.AddProject<Projects.User_API>("user-api", launchProfileName: null)
    .WithEndpoint(name: "Http", scheme: "http", port: appHostSettings.UserHttpPort, targetPort: appHostSettings.UserHttpPort, isProxied: false)
    .WithEndpoint(name: "Grpc", scheme: "http", port: appHostSettings.UserGrpcPort, targetPort: appHostSettings.UserGrpcPort, isProxied: false)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("Kestrel__Endpoints__Http__Url", $"http://0.0.0.0:{appHostSettings.UserHttpPort.ToString()}")
    .WithEnvironment("Kestrel__Endpoints__Http__Protocols", "Http1")
    .WithEnvironment("Kestrel__Endpoints__Grpc__Url", $"http://0.0.0.0:{appHostSettings.UserGrpcPort.ToString()}")
    .WithEnvironment("Kestrel__Endpoints__Grpc__Protocols", "Http2")
    .WithEnvironment("ConnectionStrings__DefaultConnection", userDb.Resource.ConnectionStringExpression)
    .WithReference(userDb)
    .WithEnvironment("Jwt__SigningKey", jwtSigningKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience);

var clientApi = builder.AddProject<Projects.Client_API>("client-api", launchProfileName: null)
    .WithEndpoint(name: "Http", scheme: "http", port: appHostSettings.ClientHttpPort, targetPort: appHostSettings.ClientHttpPort, isProxied: false)
    .WithEndpoint(name: "Grpc", scheme: "http", port: appHostSettings.ClientGrpcPort, targetPort: appHostSettings.ClientGrpcPort, isProxied: false)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("Kestrel__Endpoints__Http__Url", $"http://0.0.0.0:{appHostSettings.ClientHttpPort.ToString()}")
    .WithEnvironment("Kestrel__Endpoints__Http__Protocols", "Http1")
    .WithEnvironment("Kestrel__Endpoints__Grpc__Url", $"http://0.0.0.0:{appHostSettings.ClientGrpcPort.ToString()}")
    .WithEnvironment("Kestrel__Endpoints__Grpc__Protocols", "Http2")
    .WithEnvironment("ConnectionStrings__DefaultConnection", clientDb.Resource.ConnectionStringExpression)
    .WithReference(clientDb)
    .WithEnvironment("Jwt__SigningKey", jwtSigningKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience);

var gatewayApi = builder.AddProject<Projects.Helpdesk_Gateway_API>("gateway", launchProfileName: null)
    .WithEndpoint(name: "Http", scheme: "http", port: appHostSettings.GatewayHttpPort, targetPort: appHostSettings.GatewayHttpPort, isProxied: false)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("Kestrel__Endpoints__Http__Url", $"http://0.0.0.0:{appHostSettings.GatewayHttpPort.ToString()}")
    .WithEnvironment("Kestrel__Endpoints__Http__Protocols", "Http1")
    .WithEnvironment("Jwt__SigningKey", jwtSigningKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
    .WaitFor(ticketApi)
    .WaitFor(assetApi)
    .WaitFor(assignmentApi)
    .WaitFor(slaApi)
    .WaitFor(userApi)
    .WaitFor(clientApi);

var blazor = builder.AddProject<Projects.Helpdesk_Blazor>("blazor")
    .WithEndpoint(name: "Http", scheme: "http", port: appHostSettings.BlazorHttpPort, targetPort: appHostSettings.BlazorHttpPort, isProxied: false)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("Kestrel__Endpoints__Http__Url", $"http://0.0.0.0:{appHostSettings.BlazorHttpPort.ToString()}")
    .WithEnvironment("Kestrel__Endpoints__Http__Protocols", "Http1")
    .WaitFor(gatewayApi);

// Cross-service references, added after every resource exists (mirrors SuperCard.AppHost's
// style) - Aspire injects "services__<resource>__<endpoint>__0" env vars that service
// discovery (see Helpdesk.ServiceDefaults.AddServiceDiscovery) resolves at runtime.
assetApi.WithReference(assignmentApi).WaitFor(assignmentApi);
gatewayApi.WithReference(ticketApi).WithReference(assetApi).WithReference(assignmentApi).WithReference(slaApi).WithReference(userApi).WithReference(clientApi);
blazor.WithReference(gatewayApi);

// Aspire doesn't auto-open a browser for orchestrated resources (launchSettings.json's
// launchBrowser only applies when a project runs standalone), so do it ourselves once it's up.
builder.Eventing.Subscribe<ResourceReadyEvent>(blazor.Resource, async (_, ct) =>
{
    var url = $"http://localhost:{appHostSettings.BlazorHttpPort}";

    // ResourceReadyEvent fires once the process starts, which can be a beat before Kestrel
    // has actually bound the port - wait for a real TCP accept so the browser doesn't land
    // on a connection-refused page.
    for (var attempt = 0; attempt < 50; attempt++)
    {
        try
        {
            using var probe = new System.Net.Sockets.TcpClient();
            await probe.ConnectAsync("127.0.0.1", appHostSettings.BlazorHttpPort, ct);
            break;
        }
        catch
        {
            await Task.Delay(100, ct);
        }
    }

    try
    {
        // A bare URL launch is handed to the OS's default-browser handler, which Chrome/Edge
        // treat as "new tab in the existing window" - there's no flag for that. Getting an
        // actual new window means invoking the browser executable directly with --new-window.
        string[] chromeCandidates =
        [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "Application", "chrome.exe"),
        ];
        var chromePath = chromeCandidates.FirstOrDefault(File.Exists);

        if (chromePath is not null)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = chromePath,
                Arguments = $"--new-window \"{url}\"",
                UseShellExecute = false
            });
        }
        else
        {
            // Fallback: whatever the OS default browser is, opened the normal way (likely a
            // new tab rather than a new window).
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
    }
    catch
    {
        // Best-effort only; the dashboard link still works if this fails.
    }
});

builder.Build().Run();

internal sealed class AppHostSettings
{
    public int GatewayHttpPort { get; set; } = 5100;
    public int TicketHttpPort { get; set; } = 5101;
    public int TicketGrpcPort { get; set; } = 5111;
    public int AssetHttpPort { get; set; } = 5102;
    public int AssetGrpcPort { get; set; } = 5112;
    public int AssignmentHttpPort { get; set; } = 5103;
    public int AssignmentGrpcPort { get; set; } = 5113;
    public int SlaHttpPort { get; set; } = 5104;
    public int SlaGrpcPort { get; set; } = 5114;
    public int UserHttpPort { get; set; } = 5105;
    public int UserGrpcPort { get; set; } = 5115;
    public int ClientHttpPort { get; set; } = 5106;
    public int ClientGrpcPort { get; set; } = 5116;
    public int BlazorHttpPort { get; set; } = 5000;
}
