using FluentValidation;
using RondiTrack.DTOs.Contributions;

namespace RondiTrack.Validators;

public class RecordContributionRequestValidator
    : AbstractValidator<RecordContributionRequest>
{
    public RecordContributionRequestValidator()
    {
        RuleFor(x => x.Cycle).GreaterThan(0);
    }
}