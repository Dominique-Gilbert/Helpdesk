using Grpc.AspNetCore.Server;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Helpdesk.ServiceDefaults;

/// <summary>
/// The Supercard <c>ExceptionInterceptor</c> equivalent: one place, shared by every backend,
/// that logs the start and end of every unary RPC and turns an unhandled exception into a
/// clean <see cref="StatusCode.Internal"/> instead of leaking a raw .NET stack trace to the
/// caller over the wire.
///
/// A service class that already threw <see cref="RpcException"/> (NotFound, InvalidArgument, ...)
/// made that choice on purpose - it passes through unchanged. Only exceptions nobody saw
/// coming get translated here.
/// </summary>
public class GrpcExceptionInterceptor(ILogger<GrpcExceptionInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        logger.LogInformation("gRPC {Method} started", context.Method);

        try
        {
            var response = await continuation(request, context);
            logger.LogInformation("gRPC {Method} completed", context.Method);
            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "gRPC {Method} failed with an unhandled exception", context.Method);
            throw new RpcException(new Status(StatusCode.Internal, "An unexpected error occurred."));
        }
    }
}

/// <summary>
/// Every backend's Program.cs calls this instead of the bare AddGrpc(), so none of them can
/// forget to register the shared interceptor - same reasoning as AddServiceDefaults().
/// </summary>
public static class GrpcServerExtensions
{
    public static IGrpcServerBuilder AddHelpdeskGrpc(this IServiceCollection services) =>
        services.AddGrpc(options => options.Interceptors.Add<GrpcExceptionInterceptor>());
}
