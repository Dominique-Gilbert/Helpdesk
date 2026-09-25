using Grpc.Net.ClientFactory;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Helpdesk.ServiceDefaults;

/// <summary>
/// Forwards the current inbound request's bearer token onto an outgoing gRPC call, so a
/// caller's identity survives every hop - Gateway -> backend, and Asset.API -> Assignment.API
/// for the one nested backend-to-backend call. Every gRPC endpoint here runs over plaintext h2c
/// (no certificates in dev/compose), and Grpc.Net.Client refuses to attach call credentials to a
/// non-TLS channel unless told this is intentional.
/// </summary>
public static class GrpcAuthForwarding
{
    public static IHttpClientBuilder AddBearerForwarding(this IHttpClientBuilder clientBuilder) =>
        clientBuilder
            .ConfigureChannel(options => options.UnsafeUseInsecureChannelCallCredentials = true)
            .AddCallCredentials((_, metadata, serviceProvider) =>
            {
                var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
                var authHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
                if (!string.IsNullOrEmpty(authHeader))
                {
                    metadata.Add("Authorization", authHeader);
                }
                return Task.CompletedTask;
            });
}
