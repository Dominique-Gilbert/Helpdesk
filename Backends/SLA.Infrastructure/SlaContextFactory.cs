using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Helpdesk.Sla.Infrastructure;

public class SlaContextFactory : IDesignTimeDbContextFactory<SlaContext>
{
    public SlaContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("SLA_DB_CONNECTION")
            ?? "Server=127.0.0.1,1433;Database=helpdesk_sla;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=true;";

        var options = new DbContextOptionsBuilder<SlaContext>()
            .UseSqlServer(connection)
            .Options;

        return new SlaContext(options);
    }
}
