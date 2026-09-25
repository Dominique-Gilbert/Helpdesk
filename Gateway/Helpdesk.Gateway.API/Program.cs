using Grpc.Core;
using Helpdesk.Gateway.Interfacing;
using Helpdesk.ServiceDefaults;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddHelpdeskJwtAuth();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

// All four gRPC clients, their addresses, the DTO mapping profile and the interfacing
// implementations - one call, defined next to the things it wires.
builder.Services.AddHelpdeskInterfacing();

var app = builder.Build();

// gRPC faults must not surface as raw 500s with a stack trace attached.
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;

    var (status, title) = error switch
    {
        RpcException { StatusCode: StatusCode.InvalidArgument } => (StatusCodes.Status400BadRequest, "Invalid request"),
        RpcException { StatusCode: StatusCode.Unauthenticated } => (StatusCodes.Status401Unauthorized, "Invalid credentials"),
        RpcException { StatusCode: StatusCode.NotFound } => (StatusCodes.Status404NotFound, "Not found"),
        RpcException { StatusCode: StatusCode.AlreadyExists } => (StatusCodes.Status409Conflict, "Already exists"),
        RpcException { StatusCode: StatusCode.PermissionDenied } => (StatusCodes.Status403Forbidden, "Not allowed"),
        RpcException { StatusCode: StatusCode.FailedPrecondition } => (StatusCodes.Status409Conflict, "Not allowed in the current state"),
        RpcException { StatusCode: StatusCode.Unavailable } => (StatusCodes.Status503ServiceUnavailable, "A backend service is unavailable"),
        _ => (StatusCodes.Status500InternalServerError, "Unexpected error")
    };

    var detail = error is RpcException rpc ? rpc.Status.Detail : "See the gateway logs for details.";

    context.Response.StatusCode = status;
    await context.Response.WriteAsJsonAsync(new { title, status, detail });
}));

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
