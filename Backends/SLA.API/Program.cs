using Helpdesk.Contracts.Events;
using Helpdesk.Messaging;
using Helpdesk.Persistence;
using Helpdesk.ServiceDefaults;
using Helpdesk.Sla.Api.Consumers;
using Helpdesk.Sla.Api.Model;
using Helpdesk.Sla.Api.Services;
using Helpdesk.Sla.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddHelpdeskJwtAuth();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

builder.Services.AddDbContext<SlaContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<SlaContext>("sla-db", tags: [Extensions.ReadyTag]);

builder.Services.AddAutoMapper(typeof(MapperConfig).Assembly);
builder.Services.AddHelpdeskGrpc();

builder.AddHelpdeskPulsar();
builder.AddPulsarConsumer<TicketCreated, SlaTicketCreatedConsumer>();
builder.AddPulsarConsumer<TicketClosed, SlaTicketClosedConsumer>();
builder.AddPulsarConsumer<TicketReopened, SlaTicketReopenedConsumer>();
builder.Services.AddHostedService<SlaBreachMonitor>();

var app = builder.Build();

await app.MigrateDatabaseAsync<SlaContext>();

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<SlaService>();
app.MapDefaultEndpoints();
app.MapGet("/", () => "SLA.API - consumes Ticket.Created, publishes SLA.Breached.");

app.Run();
