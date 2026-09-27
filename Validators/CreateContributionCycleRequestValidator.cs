using FluentValidation;
using RondiTrack.DTOs.ContributionCycles;

namespace RondiTrack.Validators;

public class CreateContributionCycleRequestValidator
    : AbstractValidator<CreateContributionCycleRequest>
{
    public CreateContributionCycleRequestValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.StokvelId).GreaterThan(0);
        RuleFor(x => x.Number).GreaterThan(0);
        RuleFor(x => x.TargetAmount).GreaterThan(0);
    }
}