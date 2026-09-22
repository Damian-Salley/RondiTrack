using RondiTrack.Models;

namespace RondiTrack.Repositories;

public interface IRondiTrackRepository
{
    //CRUD Operations for User
    Task<IReadOnlyCollection<User>> GetUsersAsync();
    Task<User?> GetUserByIdAsync(int id);
    Task AddUserAsync(User user);
     Task<bool> UpdateUserAsync(User user);
    Task<bool> DeleteUserAsync(int id);

    //CRUD Operations for Stokvel
    Task<IReadOnlyCollection<Stokvel>> GetStokvelsAsync();
    Task<Stokvel?> GetStokvelByIdAsync(int id);
    Task AddStokvelAsync(Stokvel stokvel);
    Task<bool> UpdateStokvelAsync(Stokvel stokvel);
    Task<bool> DeleteStokvelAsync(int id);
}