namespace Helpdesk.Contracts;

/// <summary>Every persisted entity in the system carries its own audit stamps.</summary>
public interface IAuditItem
{
    DateTime CreatedAtUtc { get; set; }
    DateTime? UpdatedAtUtc { get; set; }
}
