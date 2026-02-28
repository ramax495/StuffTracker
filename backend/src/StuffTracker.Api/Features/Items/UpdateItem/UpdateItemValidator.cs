using FastEndpoints;
using FluentValidation;

namespace StuffTracker.Api.Features.Items.UpdateItem;

/// <summary>
/// Validator for UpdateItemRequest.
/// </summary>
public class UpdateItemValidator : Validator<UpdateItemRequest>
{
    public UpdateItemValidator()
    {
        RuleFor(x => x.Name)
            .MinimumLength(1)
            .When(x => x.Name != null)
            .WithMessage("Name must be at least 1 character")
            .MaximumLength(200)
            .When(x => x.Name != null)
            .WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(1)
            .When(x => x.Quantity.HasValue)
            .WithMessage("Quantity must be at least 1");
    }
}
