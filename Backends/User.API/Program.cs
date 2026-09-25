using Helpdesk.Persistence;
using Helpdesk.ServiceDefaults;
using Helpdesk.Users.Api.Security;
using Helpdesk.Users.Api.Services;
using Helpdesk.Users.Infrastructure;
using Helpdesk.Users.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddHelpdeskJwtAuth();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

builder.Services.AddDbContext<UserContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<UserContext>("user-db", tags: [Extensions.ReadyTag]);

builder.Services.AddSingleton<JwtTokenIssuer>();
builder.Services.AddHelpdeskGrpc();

var app = builder.Build();

await app.MigrateDatabaseAsync<UserContext>();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<UserContext>();
    await UserSeeder.SeedAsync(context);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<UserService>();
app.MapDefaultEndpoints();
app.MapGet("/", () => "User.API - authenticates seeded demo accounts and issues JWTs.");

app.Run();
