using Helpdesk.Contracts;

namespace Helpdesk.Assets.Domain.Model;

/// <summary>
/// Who had this asset, when, and against which ticket. TechnicianId is a plain Guid, not a
/// navigation property - the technician lives in Assignment.API's database and this service
/// must never reach into it. It is validated over gRPC at write time and then simply stored.
/// </summary>
public class AssignmentRecord : IAuditItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AssetId { get; set; }
    public Asset? Asset { get; set; }

    public Guid? TicketId { get; set; }
    public Guid? TechnicianId { get; set; }

    public string AssignedTo { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReturnedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public bool IsOpen => ReturnedAtUtc is null;
}
