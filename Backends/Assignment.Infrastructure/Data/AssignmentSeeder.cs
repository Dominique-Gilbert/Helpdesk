using Helpdesk.Assignments.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Assignments.Infrastructure.Data;

/// <summary>
/// Routing cannot demonstrate anything against an empty technician table, so the service
/// seeds a starting roster once. Guarded on existence: safe to run on every boot, and it
/// never overwrites data someone has since edited.
/// </summary>
public static class AssignmentSeeder
{
    public static async Task SeedAsync(AssignmentContext context, CancellationToken ct = default)
    {
        if (!await context.Technicians.AnyAsync(ct))
        {
            // Fixed, not Guid.NewGuid()-generated: User.API's UserSeeder links its seeded
            // "technician" demo login to Ayanda Nkosi's Id here by TechnicianId, not by name
            // match. That link only holds up on a fresh environment if this Id is deterministic.
            context.Technicians.AddRange(
                new Technician { Id = Guid.Parse("D176B3A9-2C03-4062-BD95-18DA6155AA47"), FullName = "Ayanda Nkosi", Email = "ayanda@helpdesk.local", Team = "Hardware", Skills = ["laptops", "printers", "keyboard"], MaxConcurrent = 5 },
                new Technician { Id = Guid.Parse("9F82A821-CA3B-4C22-9471-C4B8835F7C2A"), FullName = "Bryan de Wet", Email = "bryan@helpdesk.local", Team = "Hardware", Skills = ["desktops", "peripherals"], MaxConcurrent = 5 },
                new Technician { Id = Guid.Parse("7F4534E4-A4FA-4E53-857D-62AA3A376B54"), FullName = "Chloe Adams", Email = "chloe@helpdesk.local", Team = "Software", Skills = ["office", "licensing"], MaxConcurrent = 6 },
                new Technician { Id = Guid.Parse("12F4F92A-A28F-4676-B91F-25DAA117BFA9"), FullName = "Dineo Molefe", Email = "dineo@helpdesk.local", Team = "Network", Skills = ["switching", "vpn"], MaxConcurrent = 4 },
                new Technician { Id = Guid.Parse("83D93EF4-D950-4E8E-B6D0-76B0787201BA"), FullName = "Elias Botha", Email = "elias@helpdesk.local", Team = "Escalations", Skills = ["incident-command", "server"], MaxConcurrent = 3 },
                new Technician { Id = Guid.Parse("09D38747-A095-4D47-926D-7C6A3963FC63"), FullName = "Farai Chikafu", Email = "farai@helpdesk.local", Team = "Service Desk", Skills = ["triage", "access"], MaxConcurrent = 8 });
        }

        // Only Hardware, Software and Network have any leveled technicians - Access and
        // Facilities (and anything else) fall through to RoutingRuleMatcher instead, per the
        // routing brief: level+keyword routing is primary only where levels exist.
        if (!await context.TechnicianCategoryLevels.AnyAsync(ct))
        {
            context.TechnicianCategoryLevels.AddRange(
                new TechnicianCategoryLevel { TechnicianId = Guid.Parse("9F82A821-CA3B-4C22-9471-C4B8835F7C2A"), Category = "Hardware", Level = SkillLevel.Low },
                new TechnicianCategoryLevel { TechnicianId = Guid.Parse("D176B3A9-2C03-4062-BD95-18DA6155AA47"), Category = "Hardware", Level = SkillLevel.Normal },
                new TechnicianCategoryLevel { TechnicianId = Guid.Parse("83D93EF4-D950-4E8E-B6D0-76B0787201BA"), Category = "Hardware", Level = SkillLevel.Critical },
                new TechnicianCategoryLevel { TechnicianId = Guid.Parse("7F4534E4-A4FA-4E53-857D-62AA3A376B54"), Category = "Software", Level = SkillLevel.Normal },
                new TechnicianCategoryLevel { TechnicianId = Guid.Parse("12F4F92A-A28F-4676-B91F-25DAA117BFA9"), Category = "Network", Level = SkillLevel.Normal });
        }

        if (!await context.RoutingRules.AnyAsync(ct))
        {
            context.RoutingRules.AddRange(
                new RoutingRule { Name = "Critical to escalations", Category = null, MinPriority = TicketPriority.Critical, Team = "Escalations", Rank = 10 },
                new RoutingRule { Name = "Hardware faults", Category = "Hardware", MinPriority = TicketPriority.Low, Team = "Hardware", Rank = 20 },
                new RoutingRule { Name = "Software faults", Category = "Software", MinPriority = TicketPriority.Low, Team = "Software", Rank = 20 },
                new RoutingRule { Name = "Network faults", Category = "Network", MinPriority = TicketPriority.Low, Team = "Network", Rank = 20 },
                new RoutingRule { Name = "Access requests", Category = "Access", MinPriority = TicketPriority.Low, Team = "Service Desk", Rank = 30 },
                new RoutingRule { Name = "Everything else", Category = null, MinPriority = TicketPriority.Low, Team = "Service Desk", Rank = 90 });
        }

        await context.SaveChangesAsync(ct);
    }
}
