using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Helpdesk.Clients.Infrastructure;

public class ClientContextFactory : IDesignTimeDbContextFactory<ClientContext>
{
    public ClientContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("CLIENT_DB_CONNECTION")
            ?? "Server=127.0.0.1,1433;Database=helpdesk_client;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=true;";

        var options = new DbContextOptionsBuilder<ClientContext>()
            .UseSqlServer(connection)
            .Options;

        return new ClientContext(options);
    }
}
