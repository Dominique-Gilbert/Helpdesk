using Helpdesk.Contracts.Events;
using Helpdesk.Messaging;
using Helpdesk.Persistence;
using Helpdesk.ServiceDefaults;
using Helpdesk.Tickets.Api.Consumers;
using Helpdesk.Tickets.Api.Model;
using Helpdesk.Tickets.Api.Services;
using Helpdesk.Tickets.Infrastructure;
using Helpdesk.Tickets.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddHelpdeskJwtAuth();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

builder.Services.AddDbContext<TicketContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<TicketRepo>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<TicketContext>("ticket-db", tags: [Extensions.ReadyTag]);

builder.Services.AddAutoMapper(typeof(MapperConfig).Assembly);
builder.Services.AddHelpdeskGrpc();

// Ticket.API both publishes (Ticket.Created, Ticket.Closed, Ticket.Reopened) and consumes
// (SLA.Breached).
builder.AddHelpdeskPulsar();
builder.AddPulsarConsumer<SlaBreached, TicketSlaBreachedConsumer>();
builder.Services.AddHostedService<TicketCompletionMonitor>();

var app = builder.Build();

await app.MigrateDatabaseAsync<TicketContext>();

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<TicketService>();
app.MapDefaultEndpoints();
app.MapGet("/", () => "Ticket.API - gRPC on the :8081 endpoint. Health on /health/readiness.");

app.Run();
