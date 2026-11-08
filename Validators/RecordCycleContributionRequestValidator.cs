using FluentValidation;
using RondiTrack.DTOs.Contributions;
using RondiTrack.Models;

namespace RondiTrack.Validators;

// Validates requests to record contributions for a stokvel cycle.
public class RecordCycleContributionRequestValidator
    : AbstractValidator<RecordCycleContributionRequest>
{
    public RecordCycleContributionRequestValidator()
    {
        // The contribution must belong to a valid user ID.
        RuleFor(x => x.UserId)
            .GreaterThan(0);

        // Contributions must be positive and must not exceed
        // the maximum amount safely supported by the frontend.
        //
        // Amounts are received as integer cents to avoid
        // floating-point rounding errors.
        RuleFor(x => x.AmountInCents)
            .GreaterThan(0)
            .LessThanOrEqualTo(ContributionLimits.MaximumCents);
    }
}