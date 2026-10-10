namespace RondiTrack.Models;
using RondiTrack.Exceptions;

public class Stokvel
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public decimal ContributionAmount { get; private set; }

    private readonly List<StokvelMember> _members = new();

    public IReadOnlyCollection<StokvelMember> Members =>
        _members.AsReadOnly();

    private readonly List<ContributionCycle> _contributionCycles = new();

    public IReadOnlyCollection<ContributionCycle> ContributionCycles =>
        _contributionCycles.AsReadOnly();

    public Stokvel(int id, string name, decimal contributionAmount)
    {
        //ID Validation
        if (id <= 0)
        {
            throw new ArgumentException(
                "ID must be greater than 0.",
                nameof(id));
        }

        Id = id;

        //Name Validation
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Name cannot be null or empty.",
                nameof(name));
        }

        Name = name;

        //Contribution Amount Validation
        if (contributionAmount <= 0)
        {
            throw new ArgumentException(
                "Contribution amount must be greater than 0.",
                nameof(contributionAmount));
        }

        ContributionAmount = contributionAmount;
    }

    //Method to add a member to the stokvel
    public void AddMember(User user)
    {
        if (user is null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        if (_members.Any(member => member.UserId == user.Id))
        {
            throw new BusinessRuleException(
                "User is already a member of this stokvel.");
        }

        var member = new StokvelMember(
            Id,
            user.Id,
            "Member",
            DateTime.UtcNow);

        _members.Add(member);
    }

    //Method to update stokvel details
    public void UpdateDetails(string name, decimal contributionAmount)
    {
        //Name Validation
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Name cannot be null or empty.",
                nameof(name));
        }

        //Contribution Amount Validation
        if (contributionAmount <= 0)
        {
            throw new ArgumentException(
                "Contribution amount must be greater than 0.",
                nameof(contributionAmount));
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

        var member = _members
            .FirstOrDefault(member => member.UserId == user.Id);

        if (member is null)
        {
            throw new BusinessRuleException(
                "User is not a member of this stokvel.");
        }

        _members.Remove(member);
    }
}