namespace Helpdesk.Gateway.Dto;

public class DtoTechnician
{
    public Guid id { get; set; }
    public string fullName { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string team { get; set; } = string.Empty;
    public List<string> skills { get; set; } = [];
    public int maxConcurrent { get; set; }
    public int activeAssignments { get; set; }
    public bool isAvailable { get; set; }
    public bool hasCapacity { get; set; }
    public Guid? clientId { get; set; }
    public List<DtoTechnicianCategoryLevel> categoryLevels { get; set; } = [];
}

public class DtoRoutingRule
{
    public Guid id { get; set; }
    public string name { get; set; } = string.Empty;
    public string category { get; set; } = string.Empty;
    public string minPriority { get; set; } = string.Empty;
    public string team { get; set; } = string.Empty;
    public int rank { get; set; }
    public bool isActive { get; set; }
    public Guid? clientId { get; set; }
}

public class DtoAssignment
{
    public Guid id { get; set; }
    public Guid ticketId { get; set; }
    public string ticketReference { get; set; } = string.Empty;
    public Guid technicianId { get; set; }
    public string technicianName { get; set; } = string.Empty;
    public string team { get; set; } = string.Empty;
    public string routingRuleName { get; set; } = string.Empty;
    public string category { get; set; } = string.Empty;
    public string priority { get; set; } = string.Empty;
    public string explanation { get; set; } = string.Empty;
    public DateTime assignedAtUtc { get; set; }
    public Guid? clientId { get; set; }
}

public class DtoBacklogEntry
{
    public Guid id { get; set; }
    public Guid ticketId { get; set; }
    public string ticketReference { get; set; } = string.Empty;
    public string category { get; set; } = string.Empty;
    public string priority { get; set; } = string.Empty;
    public string startingLevel { get; set; } = string.Empty;
    public string commentText { get; set; } = string.Empty;
    public DateTime queuedAtUtc { get; set; }
}
