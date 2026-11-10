using FluentValidation;
using RondiTrack.DTOs.Stokvels;
using RondiTrack.Models;

namespace RondiTrack.Validators;

public class UpdateStokvelRequestValidator : AbstractValidator<UpdateStokvelRequest>
{
    public UpdateStokvelRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.ContributionAmount)
            .GreaterThan(0)
            .Must(ContributionLimits.HasAtMostTwoDecimalPlaces)
            .WithMessage("Contribution amount must have at most two decimal places.")
            .LessThanOrEqualTo(ContributionLimits.MaximumDecimalAmount);
    }
}