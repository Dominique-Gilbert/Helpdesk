using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Helpdesk.Assets.Infrastructure;

public class AssetContextFactory : IDesignTimeDbContextFactory<AssetContext>
{
    public AssetContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ASSET_DB_CONNECTION")
            ?? "Server=127.0.0.1,1433;Database=helpdesk_asset;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=true;";

        var options = new DbContextOptionsBuilder<AssetContext>()
            .UseSqlServer(connection)
            .Options;

        return new AssetContext(options);
    }
}
