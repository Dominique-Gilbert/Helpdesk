using Helpdesk.Assignments.Domain.Model;

namespace Helpdesk.Assignments.Domain.Routing;

public sealed record RoutingDecision(RoutingRule? Rule, Technician? Technician, string Explanation)
{
    public bool IsAssigned => Technician is not null;
}

/// <summary>
/// The riskiest business logic in the project, so it lives here: pure, static, no EF,
/// no gRPC, no clock. Assignment.Tests exercises it directly with hand-built data.
///
/// Order of evaluation:
///   1. Active rules only.
///   2. The rule's category must match the ticket's, or the rule is a catch-all.
///   3. The ticket's priority must be at or above the rule's minimum.
///   4. Lower Rank wins. Rank is the knob operators actually turn, so it is the primary
///      key - that is what lets "Critical to escalations" (rank 10) beat "Hardware faults"
///      (rank 20) on a critical hardware ticket. Specificity only breaks rank ties, and
///      name breaks those, so the same inputs always give the same answer.
///   5. First rule whose team has a technician with spare capacity wins. If a matching
///      rule's team is full, fall through to the next rule rather than queue.
///   6. If nothing matches, fall back to the least-loaded available technician anywhere.
/// </summary>
public static class RoutingRuleMatcher
{
    public static RoutingDecision Match(
        IEnumerable<RoutingRule> rules,
        IEnumerable<Technician> technicians,
        string? category,
        TicketPriority priority)
    {
        var pool = technicians as IReadOnlyCollection<Technician> ?? technicians.ToList();
        var ticketCategory = category ?? string.Empty;

        var candidates = rules
            .Where(r => r.IsActive)
            .Where(r => string.IsNullOrWhiteSpace(r.Category)
                        || string.Equals(r.Category, ticketCategory, StringComparison.OrdinalIgnoreCase))
            .Where(r => priority >= r.MinPriority)
            .OrderBy(r => r.Rank)
            .ThenBy(r => string.IsNullOrWhiteSpace(r.Category) ? 1 : 0)
            .ThenBy(r => r.Name, StringComparer.Ordinal)
            .ToList();

        foreach (var rule in candidates)
        {
            var technician = PickTechnician(pool, rule.Team);
            if (technician is not null)
            {
                return new RoutingDecision(
                    rule,
                    technician,
                    $"Rule '{rule.Name}' matched (category: {Describe(rule.Category)}, min priority: {rule.MinPriority}); assigned to {technician.FullName} on team {technician.Team}.");
            }
        }

        var fallback = PickTechnician(pool, team: null);
        if (fallback is not null)
        {
            var reason = candidates.Count == 0
                ? "No routing rule matched"
                : "Every matching rule's team was at capacity";

            return new RoutingDecision(
                null,
                fallback,
                $"{reason}; fell back to least-loaded available technician {fallback.FullName}.");
        }

        return new RoutingDecision(null, null, "No routing rule matched and no technician had spare capacity.");
    }

    private static Technician? PickTechnician(IEnumerable<Technician> technicians, string? team) =>
        technicians
            .Where(t => t.HasCapacity)
            .Where(t => team is null || string.Equals(t.Team, team, StringComparison.OrdinalIgnoreCase))
            .OrderBy(t => t.ActiveAssignments)
            .ThenBy(t => t.FullName, StringComparer.Ordinal)
            .FirstOrDefault();

    private static string Describe(string? category) =>
        string.IsNullOrWhiteSpace(category) ? "any" : category;
}
