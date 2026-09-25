using FluentValidation;

namespace Helpdesk.Blazor.Components.Validator;

/// <summary>
/// Shared MudBlazor/FluentValidation glue: every entity's validator below only declares its own
/// RuleFor(...) calls, rather than each one re-implementing "adapt FluentValidation to what
/// MudBlazor expects" from scratch. The individual field-level lengths mirror this app's own
/// backend HasMaxLength constraints one-for-one (see e.g. Ticket.API's TicketService.ValidateLength)
/// so a bad value is rejected in the drawer instead of round-tripping to the server for the same
/// InvalidArgument the backend would have returned anyway.
/// </summary>
public abstract class HelpdeskValidator<T> : AbstractValidator<T>
{
    /// <summary>Binds directly to a MudBlazor field's Validation parameter (e.g.
    /// Validation="validator.ValidateValue") for live, per-field feedback as the user types -
    /// works standalone on a single MudTextField, no enclosing MudForm required.</summary>
    public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
    {
        var result = await ValidateAsync(ValidationContext<T>.CreateWithOptions(
            (T)model, options => options.IncludeProperties(propertyName)));
        return result.IsValid ? [] : result.Errors.Select(e => e.ErrorMessage);
    };

    /// <summary>Synchronous whole-model check for a Save button's guard clause - the first
    /// validation error, or null when the model is valid. Drop-in replacement for the
    /// hand-rolled "if (string.IsNullOrWhiteSpace(...)) error = ..." checks this app used to
    /// have scattered across each drawer's SaveAsync.</summary>
    public string? ValidateAndGetError(T model)
    {
        var result = base.Validate(model);
        return result.IsValid ? null : result.Errors[0].ErrorMessage;
    }

    /// <summary>Adapts one property's rule(s) to MudBlazor's Func&lt;string, IEnumerable&lt;string&gt;&gt;
    /// Validation shape, for a drawer whose text fields are local scalars rather than bound
    /// directly to this DTO (unlike Supercard's forms, which bind straight to the model - see
    /// this class's own doc comment). `buildModel` takes the field's current value and returns
    /// just enough of a T to check that one property; every field validated this way in this app
    /// is self-contained (NotEmpty/MaximumLength), so a partially-built model is enough context.</summary>
    public Func<string, IEnumerable<string>> Field(Func<string, T> buildModel, string propertyName) => value =>
    {
        var result = Validate(ValidationContext<T>.CreateWithOptions(
            buildModel(value), options => options.IncludeProperties(propertyName)));
        return result.IsValid ? [] : result.Errors.Select(e => e.ErrorMessage);
    };
}
