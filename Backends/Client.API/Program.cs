using Helpdesk.Clients.Api.Model;
using Helpdesk.Clients.Api.Services;
using Helpdesk.Clients.Infrastructure;
using Helpdesk.Clients.Infrastructure.Data;
using Helpdesk.Persistence;
using Helpdesk.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddHelpdeskJwtAuth();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

builder.Services.AddDbContext<ClientContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ClientContext>("client-db", tags: [Extensions.ReadyTag]);

builder.Services.AddAutoMapper(typeof(MapperConfig).Assembly);
builder.Services.AddHelpdeskGrpc();

var app = builder.Build();

await app.MigrateDatabaseAsync<ClientContext>();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ClientContext>();
    await ClientSeeder.SeedAsync(context);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<ClientService>();
app.MapDefaultEndpoints();
app.MapGet("/", () => "Client.API - company/customer directory, exclusive to BaseRole.");

app.Run();
