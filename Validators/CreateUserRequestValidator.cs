using FluentValidation;
using RondiTrack.DTOs.Users;

namespace RondiTrack.Validators;

public class CreateUserRequestValidator
    : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        //Used for making sure that the user is not created with invalid data.

        //Validate the Id
        RuleFor(x => x.Id).GreaterThan(0);

        //Validate the first name
        RuleFor(x => x.FirstName).NotEmpty();

        //Validate the last name
        RuleFor(x => x.LastName).NotEmpty();

        //Validate the email
        RuleFor(x => x.Email).NotEmpty().EmailAddress();

        //Validate the phone number
        RuleFor(x => x.PhoneNumber).NotEmpty();

    }
}