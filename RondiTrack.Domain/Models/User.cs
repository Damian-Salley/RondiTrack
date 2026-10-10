namespace RondiTrack.Models;

public class User
{
    public int Id { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Email { get; private set; }
    public string PhoneNumber { get; private set; }

    public User(int id, string firstName, string lastName, string email, string phoneNumber)
    {
        //ID Validation
        if (id <= 0)
        {
            throw new ArgumentException("ID must be greater than 0.", nameof(id));
        }
        Id = id;

        //First Name Validation
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("First name cannot be null or empty.", nameof(firstName));
        }
        FirstName = firstName;

        //Last Name Validation
        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException("Last name cannot be null or empty.", nameof(lastName));
        }
        LastName = lastName;


        //Email Validation
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be null or empty.", nameof(email));
        }

        // Check if the email contains an '@' symbol
        if (!email.Contains('@'))
        {
            throw new ArgumentException("Email must contain an @ symbol.", nameof(email));
        }
        Email = email;

        //Phone Number Validation
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new ArgumentException("Phone number cannot be null or empty.", nameof(phoneNumber));

        }
        PhoneNumber = phoneNumber;

    }

    public void UpdateDetails(
    string firstName,
    string lastName,
    string email,
    string phoneNumber)
    {

        // Validate firstName
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("First name cannot be null or empty.", nameof(firstName));
        }

        // Validate lastName
        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException("Last name cannot be null or empty.", nameof(lastName));
        }

        // Validate email
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be null or empty.", nameof(email));
        }

        if (!email.Contains('@'))
        {
            throw new ArgumentException("Email must contain an @ symbol.", nameof(email));
        }

        // Validate phone number
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new ArgumentException("Phone number cannot be null or empty.", nameof(phoneNumber));
        }


        FirstName = firstName;


        LastName = lastName;


        Email = email;


        PhoneNumber = phoneNumber;
    }

}