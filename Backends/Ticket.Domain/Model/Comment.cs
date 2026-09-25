using Helpdesk.Contracts;

namespace Helpdesk.Tickets.Domain.Model;

public class Comment : IAuditItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    public string Author { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsInternal { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
