using Helpdesk.Assignments.Domain.Model;
using Helpdesk.Assignments.Domain.Routing;
using Helpdesk.Assignments.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Assignments.Api.Services;

/// <summary>
/// The single place a queued ticket gets re-evaluated from. A backlog entry does not sit
/// there forever - anything that could open up somewhere for it to go (a ticket closing and
/// freeing a slot, a technician being created, or a technician being updated with a new level
/// or more capacity) calls DrainAsync afterwards instead of leaving the backlog to wait solely
/// on the next ticket close.
///
/// A full sweep rather than a targeted "check just this technician" lookup - one capacity
/// change can free more than one queued ticket, and a RoutingPath.Rule entry doesn't care
/// which technician specifically changed, only whether anyone anywhere now has room.
/// Oldest-first, and each successful match mutates the same in-memory technician's
/// ActiveAssignments before moving to the next entry, so one sweep can't over-assign a
/// technician who only had room for one of several matching entries.
/// </summary>
public class BacklogDrainService(AssignmentContext db, ILogger<BacklogDrainService> logger)
{
    public async Task DrainAsync(CancellationToken ct)
    {
        var entries = await db.BacklogEntries.OrderBy(b => b.QueuedAtUtc).ToListAsync(ct);
        if (entries.Count == 0) return;

        var technicians = await db.Technicians.Include(t => t.CategoryLevels).ToListAsync(ct);
        var rules = await db.RoutingRules.AsNoTracking().Where(r => r.IsActive).ToListAsync(ct);

        var drainedAny = false;

        foreach (var entry in entries)
        {
            // Scoped to this entry's own Client on every pass - filtering the shared
            // in-memory lists rather than re-querying, so a technician's ActiveAssignments
            // mutation from an earlier entry in this same sweep is still reflected here.
            // Strict for technicians (never another company's), null-inclusive for rules
            // (a global fallback rule), same rule as AssignmentTicketCreatedConsumer.
            var candidateTechnicians = technicians.Where(t => t.ClientId == entry.ClientId).ToList();
            var candidateRules = rules.Where(r => r.ClientId == entry.ClientId || r.ClientId is null).ToList();

            var (technician, routingRuleId, explanation) = entry.RoutingPath == RoutingPath.Rule
                ? FromRuleDecision(RoutingRuleMatcher.Match(candidateRules, candidateTechnicians, entry.Category, entry.Priority))
                : FromLevelDecision(LevelRoutingMatcher.Match(candidateTechnicians, entry.Category, entry.Priority, entry.CommentText));

            if (technician is null) continue;

            db.BacklogEntries.Remove(entry);
            db.TicketAssignments.Add(new TicketAssignment
            {
                TicketId = entry.TicketId,
                TicketReference = entry.TicketReference,
                TechnicianId = technician.Id,
                RoutingRuleId = routingRuleId,
                Category = entry.Category,
                Priority = entry.Priority,
                Explanation = $"Drained from backlog (queued {entry.QueuedAtUtc:u}): {explanation}",
                AssignedAtUtc = DateTime.UtcNow
            });

            technician.ActiveAssignments += 1;
            technician.UpdatedAtUtc = DateTime.UtcNow;
            drainedAny = true;

            logger.LogInformation("Drained backlog ticket {Reference} to {Technician}", entry.TicketReference, technician.FullName);
        }

        if (drainedAny)
        {
            await db.SaveChangesAsync(ct);
        }
    }

    private static (Technician? Technician, Guid? RoutingRuleId, string Explanation) FromRuleDecision(RoutingDecision decision) =>
        (decision.Technician, decision.Rule?.Id, decision.Explanation);

    private static (Technician? Technician, Guid? RoutingRuleId, string Explanation) FromLevelDecision(LevelRoutingDecision decision) =>
        (decision.Technician, null, decision.Explanation);
}
