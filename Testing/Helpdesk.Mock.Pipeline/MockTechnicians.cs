using Helpdesk.Assignments.Domain.Model;

namespace Helpdesk.Mock.Pipeline;

/// <summary>
/// Hardcoded factories, the Ecosystem/Mock.Pipeline pattern. Tests ask for a roster and
/// tweak the one field they care about, so the intent of each test is visible in the test.
/// </summary>
public static class MockTechnicians
{
    public static Technician Hardware(string name = "Ayanda Nkosi", int active = 0, int max = 5) =>
        new() { FullName = name, Team = "Hardware", Skills = ["laptops"], ActiveAssignments = active, MaxConcurrent = max };

    public static Technician Software(string name = "Chloe Adams", int active = 0, int max = 5) =>
        new() { FullName = name, Team = "Software", Skills = ["office"], ActiveAssignments = active, MaxConcurrent = max };

    public static Technician Escalations(string name = "Elias Botha", int active = 0, int max = 3) =>
        new() { FullName = name, Team = "Escalations", Skills = ["incident-command"], ActiveAssignments = active, MaxConcurrent = max };

    public static Technician ServiceDesk(string name = "Farai Chikafu", int active = 0, int max = 8) =>
        new() { FullName = name, Team = "Service Desk", Skills = ["triage"], ActiveAssignments = active, MaxConcurrent = max };

    public static List<Technician> FullRoster() =>
        [Hardware(), Software(), Escalations(), ServiceDesk()];
}

public static class MockRoutingRules
{
    public static RoutingRule CriticalToEscalations() =>
        new() { Name = "Critical to escalations", Category = null, MinPriority = TicketPriority.Critical, Team = "Escalations", Rank = 10 };

    public static RoutingRule HardwareFaults() =>
        new() { Name = "Hardware faults", Category = "Hardware", MinPriority = TicketPriority.Low, Team = "Hardware", Rank = 20 };

    public static RoutingRule SoftwareFaults() =>
        new() { Name = "Software faults", Category = "Software", MinPriority = TicketPriority.Low, Team = "Software", Rank = 20 };

    public static RoutingRule CatchAll() =>
        new() { Name = "Everything else", Category = null, MinPriority = TicketPriority.Low, Team = "Service Desk", Rank = 90 };

    public static List<RoutingRule> Standard() =>
        [CriticalToEscalations(), HardwareFaults(), SoftwareFaults(), CatchAll()];
}
