namespace Helpdesk.Blazor.Services;

/// <summary>
/// Scoped per Blazor circuit - BaseRole's single "view as Client X" switch, shared by every
/// scoped page (Tickets/Assets/Technicians/Users) so picking a Client once in the profile menu
/// narrows all of them together, rather than each page carrying its own separate filter. An
/// Admin/Support/Technician never sees the switcher at all (MainLayout gates it to BaseRole) -
/// their own view is always scoped to their own Client server-side regardless of this context.
/// The Clients page (BaseRole's own Client directory) deliberately never reads this - it always
/// shows every Client, unaffected by whichever one is currently selected here.
/// </summary>
public class BaseRoleViewContext
{
    public Guid? SelectedClientId { get; private set; }
    public string? SelectedClientName { get; private set; }

    public event Action? OnChange;

    public void SetClient(Guid? clientId, string? clientName)
    {
        SelectedClientId = clientId;
        SelectedClientName = clientName;
        OnChange?.Invoke();
    }
}
