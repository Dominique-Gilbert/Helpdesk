namespace Helpdesk.Gateway.Dto;

public class DtoSlaClock
{
    public Guid id { get; set; }
    public Guid ticketId { get; set; }
    public string ticketReference { get; set; } = string.Empty;
    public string priority { get; set; } = string.Empty;
    public string status { get; set; } = string.Empty;
    public DateTime startedAtUtc { get; set; }
    public DateTime dueAtUtc { get; set; }
    public DateTime? stoppedAtUtc { get; set; }
    public DateTime? breachedAtUtc { get; set; }
    public long remainingSeconds { get; set; }
    public int escalationLevel { get; set; }
    public List<DtoEscalation> escalations { get; set; } = [];
    public List<DtoSlaAdjustment> adjustments { get; set; } = [];
}

public class DtoEscalation
{
    public Guid id { get; set; }
    public Guid slaClockId { get; set; }
    public int level { get; set; }
    public string notes { get; set; } = string.Empty;
    public DateTime raisedAtUtc { get; set; }
}

public class DtoSlaAdjustment
{
    public Guid id { get; set; }
    public Guid slaClockId { get; set; }
    public int deltaMinutes { get; set; }
    public string reason { get; set; } = string.Empty;
    public string adjustedBy { get; set; } = string.Empty;
    public DateTime adjustedAtUtc { get; set; }
}
