using FluentValidation;
using Helpdesk.Gateway.Dto;

namespace Helpdesk.Blazor.Components.Validator;

/// <summary>Mirrors Client.API's ClientContext lengths - see ClientService.CreateClient's own
/// ValidateLength calls.</summary>
public class CreateClientValidator : HelpdeskValidator<CreateClientDto>
{
    public CreateClientValidator()
    {
        RuleFor(x => x.company)
            .NotEmpty().WithMessage("Company is required.")
            .MaximumLength(128).WithMessage("Company must not exceed 128 characters.");

        RuleFor(x => x.name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(128).WithMessage("Name must not exceed 128 characters.");

        RuleFor(x => x.email)
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

        RuleFor(x => x.contactNumber)
            .MaximumLength(32).WithMessage("Contact number must not exceed 32 characters.");

        RuleFor(x => x.description)
            .MaximumLength(512).WithMessage("Description must not exceed 512 characters.");

        RuleFor(x => x.aboutInfo)
            .MaximumLength(4000).WithMessage("About info must not exceed 4000 characters.");

        RuleFor(x => x.primaryColor)
            .MaximumLength(9).WithMessage("Primary color must not exceed 9 characters.");

        RuleFor(x => x.logoUrl)
            .MaximumLength(2048).WithMessage("Logo URL must not exceed 2048 characters.");
    }
}

/// <summary>Same rules as CreateClientValidator - company/name stay required on update too,
/// matching the drawer's existing "Company is required." / "Name is required." checks.</summary>
public class UpdateClientValidator : HelpdeskValidator<UpdateClientDto>
{
    public UpdateClientValidator()
    {
        RuleFor(x => x.company)
            .NotEmpty().WithMessage("Company is required.")
            .MaximumLength(128).WithMessage("Company must not exceed 128 characters.");

        RuleFor(x => x.name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(128).WithMessage("Name must not exceed 128 characters.");

        RuleFor(x => x.email)
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

        RuleFor(x => x.contactNumber)
            .MaximumLength(32).WithMessage("Contact number must not exceed 32 characters.");

        RuleFor(x => x.description)
            .MaximumLength(512).WithMessage("Description must not exceed 512 characters.");

        RuleFor(x => x.aboutInfo)
            .MaximumLength(4000).WithMessage("About info must not exceed 4000 characters.");

        RuleFor(x => x.primaryColor)
            .MaximumLength(9).WithMessage("Primary color must not exceed 9 characters.");

        RuleFor(x => x.logoUrl)
            .MaximumLength(2048).WithMessage("Logo URL must not exceed 2048 characters.");
    }
}
