using PasswordlessOTP.API.Models;
using PasswordlessOTP.API.DTOs;

namespace PasswordlessOTP.API.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> AuthenticateUserAsync(string identifier, string otpCode);
        Task<AuthResponse> RefreshTokenAsync(string refreshToken);
        Task<bool> RevokeTokenAsync(string refreshToken);
        Task<bool> ValidateTokenAsync(string accessToken);
        Task<UserSession> CreateUserSessionAsync(Guid userId, string userAgent, string ipAddress);
        Task<bool> UpdateSessionLastUsedAsync(string sessionId);
    }
}
