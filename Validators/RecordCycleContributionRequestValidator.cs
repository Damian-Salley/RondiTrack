using FluentValidation;
using RondiTrack.DTOs.Contributions;

namespace RondiTrack.Validators;

public class RecordCycleContributionRequestValidator
    : AbstractValidator<RecordCycleContributionRequest>
{
    public RecordCycleContributionRequestValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.AmountInCents).GreaterThan(0);
    }
}