using Helpdesk.Assets.Api.Model;
using Helpdesk.Assets.Api.Services;
using Helpdesk.Assets.Infrastructure;
using Helpdesk.Assets.Infrastructure.Data;
using Helpdesk.Assignments.Grpc;
using Helpdesk.Persistence;
using Helpdesk.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddHelpdeskJwtAuth();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

builder.Services.AddDbContext<AssetContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AssetContext>("asset-db", tags: [Extensions.ReadyTag]);

builder.Services.AddAutoMapper(typeof(MapperConfig).Assembly);
builder.Services.AddHelpdeskGrpc();

// Resolved via Aspire service discovery - "_grpc.assignment-api" picks the Grpc-named
// endpoint on that resource, since it also exposes a plain Http one.
builder.Services.AddGrpcClient<AssignmentGrpc.AssignmentGrpcClient>(options =>
{
    options.Address = new Uri("http://_grpc.assignment-api");
}).AddBearerForwarding();

var app = builder.Build();

await app.MigrateDatabaseAsync<AssetContext>();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AssetContext>();
    await AssetSeeder.SeedAsync(context);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<AssetService>();
app.MapDefaultEndpoints();
app.MapGet("/", () => "Asset.API - equipment inventory. Validates technicians against Assignment.API over gRPC.");

app.Run();
