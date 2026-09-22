using RondiTrack.Models;

namespace RondiTrack.Repositories;

public class InMemoryRondiTrackRepository : IRondiTrackRepository

{
    //CRUD Operations for User
    //Get all users
    public Task<IReadOnlyCollection<User>> GetUsersAsync()
    {
        IReadOnlyCollection<User> users = _users.AsReadOnly();

        return Task.FromResult(users);
    }

    //Get user by ID
    public Task<User?> GetUserByIdAsync(int id)
    {
        var user = _users.FirstOrDefault(user => user.Id == id);

        return Task.FromResult(user);
    }

    //Add a new user
    public Task AddUserAsync(User user)
    {
        _users.Add(user);

        return Task.CompletedTask;
    }

    //Update an existing user
    public Task<bool> UpdateUserAsync(User user)
    {
        var index = _users.FindIndex(existingUser => existingUser.Id == user.Id);

        if (index == -1)
        {
            return Task.FromResult(false);
        }

        _users[index] = user;

        return Task.FromResult(true);
    }

    //Delete a user by ID
    public Task<bool> DeleteUserAsync(int id)
    {
        var user = _users.FirstOrDefault(user => user.Id == id);

        if (user is null)
        {
            return Task.FromResult(false);
        }

        _users.Remove(user);

        return Task.FromResult(true);
    }

    private readonly List<User> _users;
    private readonly List<Stokvel> _stokvels;

    //Constructor to initialize the in-memory repository with some sample data
    public InMemoryRondiTrackRepository()
    {
        _users = new List<User>
        {
            new User(1, "Damian", "Salley", "damian@example.com", "0821234567"),
            new User(2, "Tim", "Huang", "tim@example.com", "0831234567"),
            new User(3, "Ryan", "Smith", "ryan@example.com", "0841234567")
        };

        var stokvel1 = new Stokvel(1, "Community Savings", 500m);

        stokvel1.AddMember(_users[0]);
        stokvel1.AddMember(_users[1]);
        stokvel1.AddMember(_users[2]);

        _stokvels = new List<Stokvel>
        {
            stokvel1
        };
    }
    public Task<Stokvel?> GetStokvelByIdAsync(int id)
    {
        var stokvel = _stokvels.FirstOrDefault(stokvel => stokvel.Id == id);

        return Task.FromResult(stokvel);
    }

    public Task<IReadOnlyCollection<Stokvel>> GetStokvelsAsync()
    {
        IReadOnlyCollection<Stokvel> stokvels = _stokvels.AsReadOnly();

        return Task.FromResult(stokvels);
    }

    public Task AddStokvelAsync(Stokvel stokvel)
    {
        _stokvels.Add(stokvel);

        return Task.CompletedTask;
    }

    public Task<bool> UpdateStokvelAsync(Stokvel stokvel)
    {
        var index = _stokvels.FindIndex(existingStokvel => existingStokvel.Id == stokvel.Id);

        if (index == -1)
        {
            return Task.FromResult(false);
        }

        _stokvels[index] = stokvel;

        return Task.FromResult(true);
    }

    public Task<bool> DeleteStokvelAsync(int id)
    {
        var stokvel = _stokvels.FirstOrDefault(stokvel => stokvel.Id == id);

        if (stokvel is null)
        {
            return Task.FromResult(false);
        }

        _stokvels.Remove(stokvel);

        return Task.FromResult(true);
    }
}