using FastEndpoints;
using FluentValidation;

namespace StuffTracker.Api.Features.Items.MoveItem;

/// <summary>
/// Validator for MoveItemRequest.
/// </summary>
public class MoveItemValidator : Validator<MoveItemRequest>
{
    public MoveItemValidator()
    {
        RuleFor(x => x.LocationId)
            .NotEmpty()
            .WithMessage("LocationId is required");
    }
}
