using PasswordlessOTP.API.Models;
using PasswordlessOTP.API.DTOs;

namespace PasswordlessOTP.API.Services
{
    public interface IUserService
    {
        Task<User?> GetUserByIdAsync(Guid id);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByPhoneAsync(string phoneNumber);
        Task<User> CreateUserAsync(UserRegistrationRequest request);
        Task<User> UpdateUserAsync(Guid id, UserRegistrationRequest request);
        Task<bool> DeleteUserAsync(Guid id);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<bool> IsUserActiveAsync(Guid id);
        Task UpdateLastLoginAsync(Guid id);
    }
}
