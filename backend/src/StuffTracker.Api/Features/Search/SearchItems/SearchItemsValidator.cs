using FastEndpoints;
using FluentValidation;

namespace StuffTracker.Api.Features.Search.SearchItems;

/// <summary>
/// Validator for search items request.
/// </summary>
public class SearchItemsValidator : Validator<SearchItemsRequest>
{
    public SearchItemsValidator()
    {
        RuleFor(x => x.Query)
            .MaximumLength(100)
            .WithMessage("Query must not exceed 100 characters")
            .MinimumLength(1)
            .When(x => x.Query != null)
            .WithMessage("Query must be at least 1 character");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 100)
            .WithMessage("Limit must be between 1 and 100");

        RuleFor(x => x.Offset)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Offset must be 0 or greater");
    }
}
