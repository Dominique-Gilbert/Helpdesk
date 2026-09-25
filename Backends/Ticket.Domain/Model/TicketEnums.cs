namespace Helpdesk.Tickets.Domain.Model;

public enum TicketStatus
{
    New = 0,
    Assigned = 1,
    InProgress = 2,
    OnHold = 3,
    Resolved = 4,
    Closed = 5,

    /// <summary>A comment landed while the ticket was still within its 24h post-close grace
    /// window - back on someone's plate, with a freshly restarted SLA clock.</summary>
    Reopened = 6,

    /// <summary>Closed for 24h with no follow-up comment. Terminal: nothing reopens this one.</summary>
    Completed = 7
}

/// <summary>Numeric order matters: SLA targets and routing rules both compare on it.</summary>
public enum TicketPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Critical = 4
}

public enum TicketCategory
{
    General = 0,
    Hardware = 1,
    Software = 2,
    Network = 3,
    Access = 4,
    Facilities = 5
}
