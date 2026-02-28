using FastEndpoints;
using FluentValidation;

namespace StuffTracker.Api.Features.Items.CreateItem;

/// <summary>
/// Validator for CreateItemRequest.
/// </summary>
public class CreateItemValidator : Validator<CreateItemRequest>
{
    public CreateItemValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MinimumLength(1)
            .WithMessage("Name must be at least 1 character")
            .MaximumLength(200)
            .WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.LocationId)
            .NotEmpty()
            .WithMessage("LocationId is required");

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(1)
            .When(x => x.Quantity.HasValue)
            .WithMessage("Quantity must be at least 1");
    }
}
