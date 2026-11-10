using FluentValidation;
using RondiTrack.DTOs.ContributionCycles;
using RondiTrack.Models;

namespace RondiTrack.Validators;

public class UpdateContributionCycleRequestValidator
    : AbstractValidator<UpdateContributionCycleRequest>
{
    public UpdateContributionCycleRequestValidator()
    {
        RuleFor(x => x.Number).GreaterThan(0);
        RuleFor(x => x.TargetAmount)
            .GreaterThan(0)
            .Must(ContributionLimits.HasAtMostTwoDecimalPlaces)
            .WithMessage("Target amount must have at most two decimal places.")
            .LessThanOrEqualTo(ContributionLimits.MaximumDecimalAmount);
    }
}