namespace Helpdesk.Sla.Domain.Model;

/// <summary>Own copy, same reasoning as Assignment.Domain: the event speaks strings.</summary>
public enum TicketPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Critical = 4
}

public enum SlaClockStatus
{
    Running = 0,
    Stopped = 1,
    Breached = 2
}
