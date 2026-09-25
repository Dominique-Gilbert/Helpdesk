using Helpdesk.Assets.Grpc;
using Helpdesk.Assignments.Grpc;
using Helpdesk.Clients.Grpc;
using Helpdesk.Gateway.Interfacing.Interfaces;
using Helpdesk.Gateway.Interfacing.Mapping;
using Helpdesk.Gateway.Interfacing.Services;
using Helpdesk.ServiceDefaults;
using Helpdesk.Sla.Grpc;
using Helpdesk.Tickets.Grpc;
using Helpdesk.Users.Grpc;
using Microsoft.Extensions.DependencyInjection;

namespace Helpdesk.Gateway.Interfacing;

public static class InterfacingExtensions
{
    public static IServiceCollection AddHelpdeskInterfacing(this IServiceCollection services)
    {
        // Resolved via Aspire service discovery (the host app's ConfigureHttpClientDefaults
        // already applies to clients registered here) - "_grpc.<name>" picks the Grpc-named
        // endpoint on that resource, since each backend also exposes a plain Http one.
        // Every client forwards the caller's bearer token onward - the Gateway never calls a
        // backend without one, and none of them will accept it without one.
        services.AddGrpcClient<TicketGrpc.TicketGrpcClient>(o => o.Address = new Uri("http://_grpc.ticket-api")).AddBearerForwarding();
        services.AddGrpcClient<AssetGrpc.AssetGrpcClient>(o => o.Address = new Uri("http://_grpc.asset-api")).AddBearerForwarding();
        services.AddGrpcClient<AssignmentGrpc.AssignmentGrpcClient>(o => o.Address = new Uri("http://_grpc.assignment-api")).AddBearerForwarding();
        services.AddGrpcClient<SlaGrpc.SlaGrpcClient>(o => o.Address = new Uri("http://_grpc.sla-api")).AddBearerForwarding();
        services.AddGrpcClient<UserGrpc.UserGrpcClient>(o => o.Address = new Uri("http://_grpc.user-api")).AddBearerForwarding();
        services.AddGrpcClient<ClientGrpc.ClientGrpcClient>(o => o.Address = new Uri("http://_grpc.client-api")).AddBearerForwarding();

        services.AddAutoMapper(typeof(GatewayMapperConfig).Assembly);

        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IAssignmentService, AssignmentService>();
        services.AddScoped<ISlaService, SlaService>();
        services.AddScoped<ITicketOverviewService, TicketOverviewService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IClientService, ClientService>();

        return services;
    }
}
