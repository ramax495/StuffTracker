using FastEndpoints;
using FluentValidation;

namespace StuffTracker.Api.Features.Locations.UpdateLocation;

/// <summary>
/// Validator for UpdateLocationRequest.
/// </summary>
public class UpdateLocationValidator : Validator<UpdateLocationRequest>
{
    public UpdateLocationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MinimumLength(1)
            .WithMessage("Name must be at least 1 character")
            .MaximumLength(200)
            .WithMessage("Name must not exceed 200 characters");
    }
}
