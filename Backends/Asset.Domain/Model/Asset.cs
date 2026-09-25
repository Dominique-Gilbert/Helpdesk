using Helpdesk.Contracts;

namespace Helpdesk.Assets.Domain.Model;

public class Asset : IAuditItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The sticker on the device. Unique, and how humans refer to it.</summary>
    public string Tag { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;

    public AssetType Type { get; set; } = AssetType.Laptop;
    public AssetStatus Status { get; set; } = AssetStatus.InStock;

    /// <summary>The creating caller's own Client at creation time - set from the caller's JWT,
    /// never client-supplied. Null for a caller with no Client (BaseRole) and for assets created
    /// before this field existed. Scopes an Admin/Support/Technician's own view to their company -
    /// see the Gateway's AssetService.ListAsync.</summary>
    public Guid? ClientId { get; set; }

    public DateTime? PurchasedOnUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public List<AssignmentRecord> AssignmentRecords { get; set; } = [];
}
