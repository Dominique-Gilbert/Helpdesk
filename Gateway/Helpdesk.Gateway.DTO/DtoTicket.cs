namespace Helpdesk.Gateway.Dto;

/// <summary>
/// Hand-written, and deliberately not 1:1 with TicketResponse. This is the shape the
/// screen wants: real Guids and DateTimes instead of strings and Timestamps, and room for
/// fields the wire message has no business carrying.
///
/// Field casing matches Supercard.DTO's convention (lowercase-led property names, not
/// PascalCase) - the whole point of this layer is to look and feel like a Supercard domain,
/// not a stock ASP.NET one.
/// </summary>
public class DtoTicket
{
    public Guid id { get; set; }
    public string reference { get; set; } = string.Empty;
    public string title { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public string requestedBy { get; set; } = string.Empty;
    public Guid? requestedByUserId { get; set; }
    public string requestedByUsername { get; set; } = string.Empty;
    public string requestedByRole { get; set; } = string.Empty;
    public Guid? clientId { get; set; }
    public string category { get; set; } = string.Empty;
    public string priority { get; set; } = string.Empty;
    public string status { get; set; } = string.Empty;
    public bool slaBreached { get; set; }
    public DateTime? slaBreachedAtUtc { get; set; }
    public DateTime? closedAtUtc { get; set; }
    public DateTime createdAtUtc { get; set; }
    public DateTime? updatedAtUtc { get; set; }
    public List<DtoComment> comments { get; set; } = [];
}

public class DtoComment
{
    public Guid id { get; set; }
    public Guid ticketId { get; set; }
    public string author { get; set; } = string.Empty;
    public string body { get; set; } = string.Empty;
    public bool isInternal { get; set; }
    public DateTime createdAtUtc { get; set; }
}

public class DtoTicketPage
{
    public List<DtoTicket> items { get; set; } = [];
    public int total { get; set; }
    public int page { get; set; }
    public int pageSize { get; set; }
}
