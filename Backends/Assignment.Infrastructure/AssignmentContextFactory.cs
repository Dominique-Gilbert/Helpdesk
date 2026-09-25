using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Helpdesk.Assignments.Infrastructure;

public class AssignmentContextFactory : IDesignTimeDbContextFactory<AssignmentContext>
{
    public AssignmentContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ASSIGNMENT_DB_CONNECTION")
            ?? "Server=127.0.0.1,1433;Database=helpdesk_assignment;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=true;";

        var options = new DbContextOptionsBuilder<AssignmentContext>()
            .UseSqlServer(connection)
            .Options;

        return new AssignmentContext(options);
    }
}
