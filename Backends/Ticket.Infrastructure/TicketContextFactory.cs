using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Helpdesk.Tickets.Infrastructure;

/// <summary>
/// Design-time factory so `dotnet ef migrations add` works against this class library
/// without spinning up Ticket.API. It never connects - EF only needs the provider to
/// build the model - so the connection string here is a local-dev default, not a secret.
/// </summary>
public class TicketContextFactory : IDesignTimeDbContextFactory<TicketContext>
{
    public TicketContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("TICKET_DB_CONNECTION")
            ?? "Server=127.0.0.1,1433;Database=helpdesk_ticket;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=true;";

        var options = new DbContextOptionsBuilder<TicketContext>()
            .UseSqlServer(connection)
            .Options;

        return new TicketContext(options);
    }
}
