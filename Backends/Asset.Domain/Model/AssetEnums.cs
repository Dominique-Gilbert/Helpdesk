namespace Helpdesk.Assets.Domain.Model;

public enum AssetType
{
    Laptop = 0,
    Desktop = 1,
    Monitor = 2,
    Phone = 3,
    Printer = 4,
    NetworkDevice = 5,
    Peripheral = 6
}

public enum AssetStatus
{
    InStock = 0,
    Assigned = 1,
    InRepair = 2,
    Retired = 3
}
