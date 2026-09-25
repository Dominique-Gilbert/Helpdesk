using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Helpdesk.Users.Infrastructure;

public class UserContextFactory : IDesignTimeDbContextFactory<UserContext>
{
    public UserContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("USER_DB_CONNECTION")
            ?? "Server=127.0.0.1,1433;Database=helpdesk_user;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=true;";

        var options = new DbContextOptionsBuilder<UserContext>()
            .UseSqlServer(connection)
            .Options;

        return new UserContext(options);
    }
}
