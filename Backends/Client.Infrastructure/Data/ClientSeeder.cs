using Helpdesk.Clients.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Clients.Infrastructure.Data;

public static class ClientSeeder
{
    public static async Task SeedAsync(ClientContext context, CancellationToken ct = default)
    {
        if (await context.Clients.AnyAsync(ct)) return;

        context.Clients.AddRange(
            new Client
            {
                Company = "Vantage Logistics",
                Name = "Sipho Dlamini",
                Email = "sipho.dlamini@vantage.example",
                ContactNumber = "+27 11 555 0101",
                Description = "Primary IT support contract.",
                AboutInfo = "Long-standing client on the Gold SLA tier - see SLA board for current clocks."
            },
            new Client
            {
                Company = "Northwind Traders",
                Name = "Lerato Mokoena",
                Email = "lerato@northwind.example",
                ContactNumber = "+27 21 555 0142",
                Description = "Retail chain, 12 branch offices.",
                AboutInfo = "Onboarded 2026-06; escalation contact is the branch manager on duty."
            });

        await context.SaveChangesAsync(ct);
    }
}
