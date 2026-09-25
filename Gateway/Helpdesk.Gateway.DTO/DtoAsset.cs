namespace Helpdesk.Gateway.Dto;

public class DtoAsset
{
    public Guid id { get; set; }
    public string tag { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public string serialNumber { get; set; } = string.Empty;
    public string location { get; set; } = string.Empty;
    public string type { get; set; } = string.Empty;
    public string status { get; set; } = string.Empty;
    public DateTime? purchasedOnUtc { get; set; }
    public DateTime createdAtUtc { get; set; }
    public Guid? clientId { get; set; }
    public List<DtoAssignmentRecord> assignmentRecords { get; set; } = [];

    /// <summary>Computed for the UI - the wire message does not carry this.</summary>
    public DtoAssignmentRecord? currentHolder =>
        assignmentRecords.FirstOrDefault(r => r.returnedAtUtc is null);
}

public class DtoAssignmentRecord
{
    public Guid id { get; set; }
    public Guid assetId { get; set; }
    public Guid? ticketId { get; set; }
    public Guid? technicianId { get; set; }
    public string assignedTo { get; set; } = string.Empty;
    public string notes { get; set; } = string.Empty;
    public DateTime assignedAtUtc { get; set; }
    public DateTime? returnedAtUtc { get; set; }
}

public class DtoAssetPage
{
    public List<DtoAsset> items { get; set; } = [];
    public int total { get; set; }
    public int page { get; set; }
    public int pageSize { get; set; }
}
