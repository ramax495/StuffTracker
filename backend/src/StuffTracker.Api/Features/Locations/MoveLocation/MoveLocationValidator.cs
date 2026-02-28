using FastEndpoints;
using FluentValidation;

namespace StuffTracker.Api.Features.Locations.MoveLocation;

/// <summary>
/// Validator for MoveLocationRequest.
/// Note: Cycle detection is handled in the endpoint since it requires database queries.
/// </summary>
public class MoveLocationValidator : Validator<MoveLocationRequest>
{
    public MoveLocationValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Location ID is required");
    }
}
