using FluentValidation;
using Helpdesk.Gateway.Dto;

namespace Helpdesk.Blazor.Components.Validator;

/// <summary>Mirrors Assignment.API's AssignmentContext lengths for RoutingRule (Name 128,
/// Category/Team 64) - see AssignmentService.CreateRoutingRule's own ValidateLength calls.
/// Team's own dropdown can never actually produce an empty or oversized value, but the rule
/// stays here anyway: it is what the drawer's Save button check used to assert by hand, and
/// this is the one place responsible for asserting it now.</summary>
public class CreateRoutingRuleValidator : HelpdeskValidator<CreateRoutingRuleDto>
{
    public CreateRoutingRuleValidator()
    {
        RuleFor(x => x.name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(128).WithMessage("Name must not exceed 128 characters.");

        RuleFor(x => x.category)
            .MaximumLength(64).WithMessage("Category must not exceed 64 characters.");

        RuleFor(x => x.team)
            .NotEmpty().WithMessage("Team is required.")
            .MaximumLength(64).WithMessage("Team must not exceed 64 characters.");
    }
}

public class UpdateRoutingRuleValidator : HelpdeskValidator<UpdateRoutingRuleDto>
{
    public UpdateRoutingRuleValidator()
    {
        RuleFor(x => x.name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(128).WithMessage("Name must not exceed 128 characters.");

        RuleFor(x => x.category)
            .MaximumLength(64).WithMessage("Category must not exceed 64 characters.");

        RuleFor(x => x.team)
            .NotEmpty().WithMessage("Team is required.")
            .MaximumLength(64).WithMessage("Team must not exceed 64 characters.");
    }
}
