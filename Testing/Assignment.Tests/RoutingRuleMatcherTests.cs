using Helpdesk.Assignments.Domain.Model;
using Helpdesk.Assignments.Domain.Routing;
using Helpdesk.Mock.Pipeline;
using Xunit;

namespace Helpdesk.Assignments.Tests;

/// <summary>
/// The riskiest logic in the project, tested at its boundary: no database, no gRPC, no bus.
/// </summary>
public class RoutingRuleMatcherTests
{
    [Fact]
    public void Hardware_ticket_goes_to_the_hardware_team()
    {
        var decision = RoutingRuleMatcher.Match(
            MockRoutingRules.Standard(),
            MockTechnicians.FullRoster(),
            "Hardware",
            TicketPriority.Normal);

        Assert.True(decision.IsAssigned);
        Assert.Equal("Hardware", decision.Technician!.Team);
        Assert.Equal("Hardware faults", decision.Rule!.Name);
    }

    [Fact]
    public void Critical_outranks_the_category_rule()
    {
        // A critical hardware ticket matches both rules. Rank decides, and the escalation
        // rule sits at 10 against the hardware rule's 20 - so escalations win.
        var decision = RoutingRuleMatcher.Match(
            MockRoutingRules.Standard(),
            MockTechnicians.FullRoster(),
            "Hardware",
            TicketPriority.Critical);

        Assert.Equal("Escalations", decision.Technician!.Team);
    }

    [Fact]
    public void An_unknown_category_falls_through_to_the_catch_all_rule()
    {
        var decision = RoutingRuleMatcher.Match(
            MockRoutingRules.Standard(),
            MockTechnicians.FullRoster(),
            "Facilities",
            TicketPriority.Normal);

        Assert.Equal("Everything else", decision.Rule!.Name);
        Assert.Equal("Service Desk", decision.Technician!.Team);
    }

    [Fact]
    public void A_full_team_is_skipped_rather_than_queued()
    {
        var technicians = new List<Technician>
        {
            MockTechnicians.Hardware(active: 5, max: 5),   // at capacity
            MockTechnicians.ServiceDesk()
        };

        var decision = RoutingRuleMatcher.Match(
            MockRoutingRules.Standard(), technicians, "Hardware", TicketPriority.Normal);

        Assert.True(decision.IsAssigned);
        Assert.Equal("Service Desk", decision.Technician!.Team);
        Assert.Equal("Everything else", decision.Rule!.Name);
    }

    [Fact]
    public void An_unavailable_technician_is_never_chosen()
    {
        var unavailable = MockTechnicians.Hardware();
        unavailable.IsAvailable = false;

        var decision = RoutingRuleMatcher.Match(
            [MockRoutingRules.HardwareFaults()], [unavailable], "Hardware", TicketPriority.High);

        Assert.False(decision.IsAssigned);
        Assert.Null(decision.Technician);
    }

    [Fact]
    public void The_least_loaded_technician_on_the_team_wins()
    {
        var technicians = new List<Technician>
        {
            MockTechnicians.Hardware("Busy Bob", active: 4),
            MockTechnicians.Hardware("Idle Ida", active: 1)
        };

        var decision = RoutingRuleMatcher.Match(
            [MockRoutingRules.HardwareFaults()], technicians, "Hardware", TicketPriority.Normal);

        Assert.Equal("Idle Ida", decision.Technician!.FullName);
    }

    [Fact]
    public void Equal_load_breaks_on_name_so_the_result_is_deterministic()
    {
        var technicians = new List<Technician>
        {
            MockTechnicians.Hardware("Zara Zulu"),
            MockTechnicians.Hardware("Adam Abrahams")
        };

        var first = RoutingRuleMatcher.Match([MockRoutingRules.HardwareFaults()], technicians, "Hardware", TicketPriority.Low);
        var second = RoutingRuleMatcher.Match([MockRoutingRules.HardwareFaults()], technicians, "Hardware", TicketPriority.Low);

        Assert.Equal("Adam Abrahams", first.Technician!.FullName);
        Assert.Equal(first.Technician.FullName, second.Technician!.FullName);
    }

    [Fact]
    public void A_rule_below_its_minimum_priority_does_not_match()
    {
        var decision = RoutingRuleMatcher.Match(
            [MockRoutingRules.CriticalToEscalations()],
            [MockTechnicians.Escalations()],
            "Hardware",
            TicketPriority.Normal);

        // The rule did not apply, but the fallback still found someone with capacity.
        Assert.Null(decision.Rule);
        Assert.True(decision.IsAssigned);
    }

    [Fact]
    public void Nothing_is_assigned_when_every_technician_is_full()
    {
        var technicians = new List<Technician>
        {
            MockTechnicians.Hardware(active: 5, max: 5),
            MockTechnicians.ServiceDesk(active: 8, max: 8)
        };

        var decision = RoutingRuleMatcher.Match(
            MockRoutingRules.Standard(), technicians, "Hardware", TicketPriority.High);

        Assert.False(decision.IsAssigned);
        Assert.Contains("capacity", decision.Explanation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void An_inactive_rule_is_ignored()
    {
        var rule = MockRoutingRules.HardwareFaults();
        rule.IsActive = false;

        var decision = RoutingRuleMatcher.Match(
            [rule, MockRoutingRules.CatchAll()],
            MockTechnicians.FullRoster(),
            "Hardware",
            TicketPriority.Normal);

        Assert.Equal("Everything else", decision.Rule!.Name);
    }
}
