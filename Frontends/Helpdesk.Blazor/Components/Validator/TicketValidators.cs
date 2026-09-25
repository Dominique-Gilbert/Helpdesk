using FluentValidation;
using Helpdesk.Gateway.Dto;

namespace Helpdesk.Blazor.Components.Validator;

/// <summary>Mirrors Ticket.API's TicketContext lengths (Title 256, Description/Comment 4000,
/// RequestedBy 128) - see TicketService.CreateTicket's own ValidateLength calls.</summary>
public class CreateTicketValidator : HelpdeskValidator<CreateTicketDto>
{
    public CreateTicketValidator()
    {
        RuleFor(x => x.title)
            .NotEmpty().WithMessage("A title is required.")
            .MaximumLength(256).WithMessage("Title must not exceed 256 characters.");

        RuleFor(x => x.description)
            .MaximumLength(4000).WithMessage("Description must not exceed 4000 characters.");

        RuleFor(x => x.requestedBy)
            .MaximumLength(128).WithMessage("Requested by must not exceed 128 characters.");

        RuleFor(x => x.comment)
            .MaximumLength(4000).WithMessage("Comment must not exceed 4000 characters.");
    }
}
