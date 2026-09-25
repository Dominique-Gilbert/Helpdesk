using FluentValidation;
using Helpdesk.Gateway.Dto;

namespace Helpdesk.Blazor.Components.Validator;

/// <summary>Mirrors Assignment.API's AssignmentContext lengths for Technician (Email 256,
/// Team 64) and the maxConcurrent >= 0 guard UserService.ValidateTechnicianDetails enforces
/// server-side - see Gateway's UserService.CreateOrUpdateTechnicianAsync. Embedded inside both
/// CreateUserValidator and UpdateUserValidator rather than standalone, since nothing in this
/// app's Blazor UI ever posts a bare CreateTechnicianDto/UpdateTechnicianDto - a Technician
/// record only ever comes from a User whose role is "Technician".</summary>
public class TechnicianDetailsValidator : AbstractValidator<TechnicianDetailsDto>
{
    public TechnicianDetailsValidator()
    {
        RuleFor(x => x.team)
            .NotEmpty().WithMessage("Team is required for a Technician.")
            .MaximumLength(64).WithMessage("Team must not exceed 64 characters.");

        RuleFor(x => x.email)
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

        RuleFor(x => x.maxConcurrent)
            .GreaterThanOrEqualTo(0).WithMessage("Max concurrent must not be negative.");
    }
}

/// <summary>Mirrors User.API's UserContext lengths (Username 64, FullName 128) and the same
/// 8-character password minimum ChangePassword already enforced - see UserService.CreateUser's
/// own ValidateLength/length checks.</summary>
public class CreateUserValidator : HelpdeskValidator<CreateUserDto>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.username)
            .NotEmpty().WithMessage("Username is required.")
            .MaximumLength(64).WithMessage("Username must not exceed 64 characters.");

        RuleFor(x => x.password)
            .NotEmpty().WithMessage("Password is required for a new user.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");

        RuleFor(x => x.fullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(128).WithMessage("Full name must not exceed 128 characters.");

        RuleFor(x => x.technician!)
            .SetValidator(new TechnicianDetailsValidator())
            .When(x => x.role == "Technician" && x.technician is not null);
    }
}

public class UpdateUserValidator : HelpdeskValidator<UpdateUserDto>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.fullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(128).WithMessage("Full name must not exceed 128 characters.");

        RuleFor(x => x.newPassword!)
            .MinimumLength(8).WithMessage("New password must be at least 8 characters.")
            .When(x => !string.IsNullOrEmpty(x.newPassword));

        RuleFor(x => x.technician!)
            .SetValidator(new TechnicianDetailsValidator())
            .When(x => x.role == "Technician" && x.technician is not null);
    }
}
