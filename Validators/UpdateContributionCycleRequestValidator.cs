using FluentValidation;
using RondiTrack.DTOs.ContributionCycles;

namespace RondiTrack.Validators;

public class UpdateContributionCycleRequestValidator
    : AbstractValidator<UpdateContributionCycleRequest>
{
    public UpdateContributionCycleRequestValidator()
    {
        RuleFor(x => x.Number).GreaterThan(0);
        RuleFor(x => x.TargetAmount).GreaterThan(0);
    }
}