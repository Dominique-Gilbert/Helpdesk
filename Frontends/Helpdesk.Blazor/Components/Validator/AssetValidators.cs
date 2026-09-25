using FluentValidation;
using Helpdesk.Gateway.Dto;

namespace Helpdesk.Blazor.Components.Validator;

/// <summary>Mirrors Asset.API's AssetContext lengths (Tag 32, Name/Location 128, SerialNumber 64)
/// - see AssetService.CreateAsset's own ValidateLength calls. This is the field that produced a
/// raw 500 straight through the "New asset" drawer before both sides got this guard.</summary>
public class CreateAssetValidator : HelpdeskValidator<CreateAssetDto>
{
    public CreateAssetValidator()
    {
        RuleFor(x => x.tag)
            .NotEmpty().WithMessage("Tag is required.")
            .MaximumLength(32).WithMessage("Tag must not exceed 32 characters.");

        RuleFor(x => x.name)
            .MaximumLength(128).WithMessage("Name must not exceed 128 characters.");

        RuleFor(x => x.serialNumber)
            .MaximumLength(64).WithMessage("Serial number must not exceed 64 characters.");

        RuleFor(x => x.location)
            .MaximumLength(128).WithMessage("Location must not exceed 128 characters.");
    }
}

/// <summary>Same lengths as CreateAssetValidator - Tag isn't here at all because it's immutable
/// after creation (UpdateAssetDto has no tag field).</summary>
public class UpdateAssetValidator : HelpdeskValidator<UpdateAssetDto>
{
    public UpdateAssetValidator()
    {
        RuleFor(x => x.name)
            .MaximumLength(128).WithMessage("Name must not exceed 128 characters.");

        RuleFor(x => x.serialNumber)
            .MaximumLength(64).WithMessage("Serial number must not exceed 64 characters.");

        RuleFor(x => x.location)
            .MaximumLength(128).WithMessage("Location must not exceed 128 characters.");
    }
}
