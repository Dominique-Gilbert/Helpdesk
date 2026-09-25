namespace Helpdesk.Gateway.Dto;

/// <summary>
/// The aggregation DTO: one object stitched from three services that know nothing about
/// each other. Nothing like this exists on the wire, which is exactly why the hand-written
/// DTO layer is separate from the generated one.
///
/// Assignment and SlaClock are nullable on purpose - immediately after a ticket is created
/// the consumers may not have run yet, and "not there yet" is a legitimate state, not an error.
/// </summary>
public class DtoTicketOverview
{
    public DtoTicket ticket { get; set; } = new();
    public DtoAssignment? assignment { get; set; }
    public DtoSlaClock? slaClock { get; set; }

    public bool isFullyProcessed => assignment is not null && slaClock is not null;
}
