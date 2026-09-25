namespace Helpdesk.Assignments.Domain.Model;

/// <summary>
/// Deliberately a copy of the enum in Ticket.Domain rather than a shared type.
/// Ticket.Created carries priority as a string; this service parses it into its own
/// vocabulary. Sharing the enum would couple two independently deployable services
/// to one another's release cadence for no benefit.
/// </summary>
public enum TicketPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// A technician's skill tier for one category - not the same scale as TicketPriority, even
/// though the names line up 1:1. LevelRoutingMatcher maps a ticket's priority to a starting
/// tier and only ever escalates upward from there (Low -> Normal -> High -> Critical), never
/// back down, per the routing brief.
/// </summary>
public enum SkillLevel
{
    Low = 1,
    Normal = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Which matcher an AssignmentBacklogEntry should be retried against once BacklogDrainService
/// sweeps the queue - the two matchers key off completely different technician attributes
/// (category level vs. team), so a queued ticket has to remember which one produced it.
/// </summary>
public enum RoutingPath
{
    Level,
    Rule
}
