using Helpdesk.Assignments.Domain.Model;
using Helpdesk.Assignments.Domain.Routing;
using Helpdesk.Mock.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Helpdesk.Assignments.Tests;

/// <summary>
/// The SupercardServices testing shape: a raw ServiceCollection rather than
/// WebApplicationFactory. It proves the logic composes under DI without booting a host,
/// a database or a broker.
/// </summary>
public class RoutingServiceProviderTests
{
    private interface IRosterSource
    {
        IReadOnlyList<Technician> Technicians { get; }
        IReadOnlyList<RoutingRule> Rules { get; }
    }

    private sealed class MockRosterSource : IRosterSource
    {
        public IReadOnlyList<Technician> Technicians { get; } = MockTechnicians.FullRoster();
        public IReadOnlyList<RoutingRule> Rules { get; } = MockRoutingRules.Standard();
    }

    [Fact]
    public void Routing_resolves_and_decides_through_a_plain_service_provider()
    {
        var provider = new ServiceCollection()
            .AddSingleton<IRosterSource, MockRosterSource>()
            .BuildServiceProvider();

        var roster = provider.GetRequiredService<IRosterSource>();

        var decision = RoutingRuleMatcher.Match(
            roster.Rules, roster.Technicians, "Software", TicketPriority.High);

        Assert.True(decision.IsAssigned);
        Assert.Equal("Software", decision.Technician!.Team);
    }
}
