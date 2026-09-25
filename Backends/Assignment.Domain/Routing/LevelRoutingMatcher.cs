using Helpdesk.Assignments.Domain.Model;

namespace Helpdesk.Assignments.Domain.Routing;

/// <summary>
/// The primary routing path for any category that has at least one technician with a
/// configured level (see LevelRoutingMatcher.HasConfiguredLevels) - RoutingRuleMatcher remains
/// the fallback for categories nobody has been leveled for (e.g. Access, Facilities).
///
/// Pure, static, no EF, no clock - same discipline as RoutingRuleMatcher.
///
/// Algorithm (per the routing brief):
///   1. Starting tier = the ticket's priority, read straight across onto SkillLevel (Low
///      priority starts at Low, Critical starts at Critical) - TicketPriority and SkillLevel
///      share the same 1-4 values by design, so this is a plain cast.
///   2. Within a tier, technicians whose Skills contains a keyword found in the ticket's
///      comment text are preferred; among those (or, if none match, among the whole tier)
///      the least-loaded technician with spare capacity wins.
///   3. If nobody in the tier has capacity, escalate to the next tier up - Low -> Normal ->
///      High -> Critical. Escalation only ever goes up, never back down, regardless of the
///      ticket's own priority.
///   4. If nobody from the starting tier through Critical has capacity, the ticket belongs in
///      the backlog - the caller is responsible for queuing an AssignmentBacklogEntry.
/// </summary>
public static class LevelRoutingMatcher
{
    /// <summary>Whether any technician has a configured level for this category - if not, the
    /// caller should fall back to RoutingRuleMatcher instead of calling Match.</summary>
    public static bool HasConfiguredLevels(IEnumerable<Technician> technicians, string category) =>
        technicians.Any(t => t.CategoryLevels.Any(l => string.Equals(l.Category, category, StringComparison.OrdinalIgnoreCase)));

    public static SkillLevel StartingLevelFor(TicketPriority priority) => (SkillLevel)(int)priority;

    public static LevelRoutingDecision Match(
        IEnumerable<Technician> technicians,
        string category,
        TicketPriority priority,
        string? commentText)
    {
        var pool = technicians as IReadOnlyCollection<Technician> ?? technicians.ToList();
        var startingLevel = StartingLevelFor(priority);
        var comment = commentText ?? string.Empty;

        for (var level = startingLevel; level <= SkillLevel.Critical; level++)
        {
            var tier = pool
                .Where(t => t.HasCapacity)
                .Where(t => t.CategoryLevels.Any(l =>
                    string.Equals(l.Category, category, StringComparison.OrdinalIgnoreCase) && l.Level == level))
                .ToList();

            if (tier.Count == 0) continue;

            var keywordMatches = tier
                .Where(t => t.Skills.Any(skill => !string.IsNullOrWhiteSpace(skill) &&
                                                   comment.Contains(skill, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(t => t.ActiveAssignments)
                .ThenBy(t => t.FullName, StringComparer.Ordinal)
                .ToList();

            var technician = keywordMatches.FirstOrDefault();
            var byKeyword = technician is not null;

            technician ??= tier
                .OrderBy(t => t.ActiveAssignments)
                .ThenBy(t => t.FullName, StringComparer.Ordinal)
                .First();

            var reason = byKeyword
                ? $"Matched a specialty keyword in the ticket comment; assigned to {technician.FullName} ({level} level, {category})."
                : $"No keyword match at {level} level; assigned to least-loaded {level}-level technician {technician.FullName}.";

            if (level != startingLevel)
            {
                reason = $"Escalated from {startingLevel} to {level} level (no capacity below). {reason}";
            }

            return new LevelRoutingDecision(technician, startingLevel, reason);
        }

        return new LevelRoutingDecision(null, startingLevel,
            $"No {category} technician from {startingLevel} level up to Critical had spare capacity; queued to backlog.");
    }
}

public sealed record LevelRoutingDecision(Technician? Technician, SkillLevel StartingLevel, string Explanation)
{
    public bool IsAssigned => Technician is not null;
}
