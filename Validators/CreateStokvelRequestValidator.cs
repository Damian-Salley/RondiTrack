using FluentValidation;
using RondiTrack.DTOs.Stokvels;
using RondiTrack.Models;

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
            .GreaterThan(0)
            .Must(ContributionLimits.HasAtMostTwoDecimalPlaces)
            .WithMessage("Contribution amount must have at most two decimal places.")
            .LessThanOrEqualTo(ContributionLimits.MaximumDecimalAmount);
    }
}