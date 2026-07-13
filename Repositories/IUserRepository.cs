using GroceryOrderingApp.Backend.Models;

namespace GroceryOrderingApp.Backend.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserByUserIdAsync(string userId);
        Task<User?> GetUserByMobileNumberAsync(string mobileNumber);
        Task<List<User>> GetAllUsersAsync();
        Task<User> CreateUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task SaveAsync();
    }
}
