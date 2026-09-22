namespace RondiTrack.Models;

public class Stokvel
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public decimal ContributionAmount { get; private set; }

    public Stokvel(int id, string name, decimal contributionAmount)
    {
        //ID Validation
        if (id <= 0)
        {
            throw new ArgumentException("ID must be greater than 0.", nameof(id));
        }
        Id = id;

        //Name Validation
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));
        }
        Name = name;

        //Contribution Amount Validation
        if (contributionAmount <= 0)
        {
            throw new ArgumentException("Contribution amount must be greater than 0.", nameof(contributionAmount));
        }
        ContributionAmount = contributionAmount;
    }

    private readonly List<User> _members = new();

    public IReadOnlyCollection<User> Members => _members.AsReadOnly();

    //Method to add a member to the stokvel
    public void AddMember(User user)
    {
        if (user is null)
        {
            throw new ArgumentNullException(nameof(user));
        }
        if (_members.Any(member => member.Id == user.Id))
        {
            throw new InvalidOperationException("User is already a member of this stokvel.");
        }

        _members.Add(user);
    }

    //Method to update stokvel details
    public void UpdateDetails(string name, decimal contributionAmount)
    {
        //Name Validation
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));
        }

        //Contribution Amount Validation
        if (contributionAmount <= 0)
        {
            throw new ArgumentException("Contribution amount must be greater than 0.", nameof(contributionAmount));
        }
        ContributionAmount = contributionAmount;
        Name = name;
    }

    //Method to remove a member from the stokvel
    public void RemoveMember(User user)
    {
        if (user is null)
        {
            throw new ArgumentNullException(nameof(user));
        }
        if (!_members.Any(member => member.Id == user.Id))
        {
            throw new InvalidOperationException("User is not a member of this stokvel.");
        }

        _members.Remove(user);
    }

}