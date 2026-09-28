using FluentValidation;
using RondiTrack.DTOs.Stokvels;

namespace RondiTrack.Validators;

public class CreateStokvelRequestValidator
    : AbstractValidator<CreateStokvelRequest>
{
    public CreateStokvelRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.Name)
            .NotEmpty();

        RuleFor(x => x.ContributionAmount)
            .GreaterThan(0);
    }
}