using Helpdesk.Assignments.Api.Consumers;
using Helpdesk.Assignments.Api.Model;
using Helpdesk.Assignments.Api.Services;
using Helpdesk.Assignments.Infrastructure;
using Helpdesk.Assignments.Infrastructure.Data;
using Helpdesk.Contracts.Events;
using Helpdesk.Messaging;
using Helpdesk.Persistence;
using Helpdesk.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddHelpdeskJwtAuth();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

builder.Services.AddDbContext<AssignmentContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AssignmentContext>("assignment-db", tags: [Extensions.ReadyTag]);

builder.Services.AddAutoMapper(typeof(MapperConfig).Assembly);
builder.Services.AddHelpdeskGrpc();
builder.Services.AddScoped<BacklogDrainService>();

builder.AddHelpdeskPulsar();
builder.AddPulsarConsumer<TicketCreated, AssignmentTicketCreatedConsumer>();
builder.AddPulsarConsumer<TicketClosed, AssignmentTicketClosedConsumer>();
builder.AddPulsarConsumer<TicketReopened, AssignmentTicketReopenedConsumer>();

var app = builder.Build();

await app.MigrateDatabaseAsync<AssignmentContext>();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AssignmentContext>();
    await AssignmentSeeder.SeedAsync(context);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<AssignmentService>();
app.MapDefaultEndpoints();
app.MapGet("/", () => "Assignment.API - consumes Ticket.Created, owns technicians and routing rules.");

app.Run();
