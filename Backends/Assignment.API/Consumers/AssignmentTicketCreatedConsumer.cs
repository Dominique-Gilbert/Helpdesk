using Helpdesk.Assignments.Domain.Model;
using Helpdesk.Assignments.Domain.Routing;
using Helpdesk.Assignments.Infrastructure;
using Helpdesk.Contracts.Events;
using Helpdesk.Mapping;
using Helpdesk.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Assignments.Api.Consumers;

/// <summary>
/// Half of the fan-out. This consumer matches a RoutingRule and assigns a Technician.
///
/// It does not know SLA.API exists, does not wait for it, and does not care whether it is
/// even running. Its class name becomes its own Pulsar subscription name, distinct from
/// SLA.API's consumer of the same topic - two independent subscriptions on one topic is what
/// makes this a fan-out rather than two services competing for the same message.
/// </summary>
public class AssignmentTicketCreatedConsumer(
    AssignmentContext db,
    ILogger<AssignmentTicketCreatedConsumer> logger) : IEventConsumer<TicketCreated>
{
    public async Task ConsumeAsync(TicketCreated message, CancellationToken ct)
    {
        // Idempotent: Pulsar guarantees at-least-once, so redelivery must be a no-op.
        if (await db.TicketAssignments.AnyAsync(a => a.TicketId == message.TicketId, ct))
        {
            logger.LogInformation("Ticket {Reference} is already assigned - skipping", message.Reference);
            return;
        }

        var priority = ProtoConverters.ToEnum(message.Priority, TicketPriority.Normal);

        // Strictly the ticket's own Client - a technician never works another company's
        // tickets, so there is no cross-company fallback here (unlike RoutingRules below).
        var technicians = await db.Technicians.Include(t => t.CategoryLevels)
            .Where(t => t.ClientId == message.ClientId)
            .ToListAsync(ct);

        // Level+keyword routing is the primary path for any category someone has been leveled
        // in; RoutingRuleMatcher (rank/team based) remains the fallback for the rest (e.g.
        // Access, Facilities) - see the routing brief for why both systems coexist.
        if (LevelRoutingMatcher.HasConfiguredLevels(technicians, message.Category))
        {
            var levelDecision = LevelRoutingMatcher.Match(technicians, message.Category, priority, message.Comment);

            if (!levelDecision.IsAssigned)
            {
                db.BacklogEntries.Add(new AssignmentBacklogEntry
                {
                    TicketId = message.TicketId,
                    TicketReference = message.Reference,
                    Category = message.Category,
                    Priority = priority,
                    StartingLevel = levelDecision.StartingLevel,
                    CommentText = message.Comment,
                    RoutingPath = RoutingPath.Level,
                    ClientId = message.ClientId,
                    QueuedAtUtc = DateTime.UtcNow
                });
                await db.SaveChangesAsync(ct);

                logger.LogWarning("Ticket {Reference} queued to backlog: {Explanation}",
                    message.Reference, levelDecision.Explanation);
                return;
            }

            await AssignAsync(message, priority, levelDecision.Technician!, routingRuleId: null, levelDecision.Explanation, ct);
            return;
        }

        // A rule with no ClientId is a platform-wide fallback (still shared across every
        // company, same as before this ticket carried one) - a rule with a real ClientId only
        // ever competes for that company's own tickets.
        var rules = await db.RoutingRules.AsNoTracking()
            .Where(r => r.ClientId == message.ClientId || r.ClientId == null)
            .ToListAsync(ct);
        var decision = RoutingRuleMatcher.Match(rules, technicians, message.Category, priority);

        if (!decision.IsAssigned)
        {
            // Everyone is busy, not an error - queue it the same way a level-routed ticket
            // would be, so BacklogDrainService can pick it up the moment anyone anywhere
            // (any team, RoutingRuleMatcher's fallback isn't team-scoped) frees a slot.
            db.BacklogEntries.Add(new AssignmentBacklogEntry
            {
                TicketId = message.TicketId,
                TicketReference = message.Reference,
                Category = message.Category,
                Priority = priority,
                CommentText = message.Comment,
                RoutingPath = RoutingPath.Rule,
                ClientId = message.ClientId,
                QueuedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync(ct);

            logger.LogWarning("Ticket {Reference} queued to backlog: {Explanation}",
                message.Reference, decision.Explanation);
            return;
        }

        await AssignAsync(message, priority, decision.Technician!, decision.Rule?.Id, decision.Explanation, ct);
    }

    private async Task AssignAsync(
        TicketCreated message,
        TicketPriority priority,
        Technician technician,
        Guid? routingRuleId,
        string explanation,
        CancellationToken ct)
    {
        db.TicketAssignments.Add(new TicketAssignment
        {
            TicketId = message.TicketId,
            TicketReference = message.Reference,
            TechnicianId = technician.Id,
            RoutingRuleId = routingRuleId,
            Category = message.Category,
            Priority = priority,
            Explanation = explanation,
            AssignedAtUtc = DateTime.UtcNow
        });

        technician.ActiveAssignments += 1;
        technician.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Assigned ticket {Reference} to {Technician}. {Explanation}",
            message.Reference, technician.FullName, explanation);
    }
}
