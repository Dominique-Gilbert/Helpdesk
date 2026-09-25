using Helpdesk.Assets.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Assets.Infrastructure.Data;

public static class AssetSeeder
{
    public static async Task SeedAsync(AssetContext context, CancellationToken ct = default)
    {
        if (await context.Assets.AnyAsync(ct)) return;

        context.Assets.AddRange(
            new Asset { Tag = "LT-0001", Name = "Dell Latitude 5540", SerialNumber = "DL5540-91A", Type = AssetType.Laptop, Location = "JHB-3F" },
            new Asset { Tag = "LT-0002", Name = "Lenovo ThinkPad T14", SerialNumber = "TP14-55C", Type = AssetType.Laptop, Location = "JHB-3F" },
            new Asset { Tag = "MN-0014", Name = "Dell U2723QE 27in", SerialNumber = "U27-2201", Type = AssetType.Monitor, Location = "JHB-2F" },
            new Asset { Tag = "PR-0003", Name = "HP LaserJet M428", SerialNumber = "LJ428-77", Type = AssetType.Printer, Location = "JHB-1F" },
            new Asset { Tag = "NW-0009", Name = "Cisco Catalyst 9200", SerialNumber = "C9200-31", Type = AssetType.NetworkDevice, Location = "Comms room" },
            new Asset { Tag = "PH-0021", Name = "iPhone 14", SerialNumber = "IP14-8842", Type = AssetType.Phone, Location = "JHB-3F" });

        await context.SaveChangesAsync(ct);
    }
}
