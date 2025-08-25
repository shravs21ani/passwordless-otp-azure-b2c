using Microsoft.EntityFrameworkCore;
using PasswordlessOTP.API.Data;
using PasswordlessOTP.API.Models;
using PasswordlessOTP.API.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PasswordlessOTP.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IOTPService _otpService;
        private readonly IUserService _userService;

        public AuthService(
            ApplicationDbContext context,
            IConfiguration configuration,
            IOTPService otpService,
            IUserService userService)
        {
            _context = context;
            _configuration = configuration;
            _otpService = otpService;
            _userService = userService;
        }

        public async Task<AuthResponse> AuthenticateUserAsync(string identifier, string otpCode)
        {
            // Validate OTP
            var otpResult = await _otpService.ValidateOTPAsync(identifier, otpCode);
            if (!otpResult.Success)
            {
                throw new UnauthorizedAccessException("Invalid OTP");
            }

            // Get user
            var user = await _userService.GetUserByEmailAsync(identifier) 
                ?? await _userService.GetUserByPhoneAsync(identifier);
            
            if (user == null || !user.IsActive)
            {
                throw new UnauthorizedAccessException("User not found or inactive");
            }

            // Update last login
            await _userService.UpdateLastLoginAsync(user.Id);

            // Create session
            var session = await CreateUserSessionAsync(user.Id, "Web", "127.0.0.1");

            // Generate tokens
            var accessToken = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            // Update session with tokens
            session.AccessToken = accessToken;
            session.RefreshToken = refreshToken;
            await _context.SaveChangesAsync();

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                User = new UserResponse
                {
                    Id = user.Id,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    FirstName = user.FirstName,
                    LastName = user.LastName
                }
            };
        }

        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken && s.IsActive);

            if (session == null)
                throw new UnauthorizedAccessException("Invalid refresh token");

            var user = await _userService.GetUserByIdAsync(session.UserId);
            if (user == null || !user.IsActive)
                throw new UnauthorizedAccessException("User not found or inactive");

            // Generate new tokens
            var newAccessToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();

            // Update session
            session.AccessToken = newAccessToken;
            session.RefreshToken = newRefreshToken;
            session.LastUsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new AuthResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                User = new UserResponse
                {
                    Id = user.Id,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    FirstName = user.FirstName,
                    LastName = user.LastName
                }
            };
        }

        public async Task<bool> RevokeTokenAsync(string refreshToken)
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken);

            if (session != null)
            {
                session.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        public async Task<bool> ValidateTokenAsync(string accessToken)
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.AccessToken == accessToken && s.IsActive);

            return session != null;
        }

        public async Task<UserSession> CreateUserSessionAsync(Guid userId, string userAgent, string ipAddress)
        {
            var session = new UserSession
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                UserAgent = userAgent,
                IPAddress = ipAddress
            };

            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }

        public async Task<bool> UpdateSessionLastUsedAsync(string sessionId)
        {
            var session = await _context.UserSessions.FindAsync(sessionId);
            if (session != null)
            {
                session.LastUsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        private string GenerateJwtToken(User user)
        {
            var key = Encoding.ASCII.GetBytes(_configuration["JWT:Secret"] ?? "your-secret-key-here");
            var tokenHandler = new JwtSecurityTokenHandler();
            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim("user_id", user.Id.ToString())
            };

            var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = _configuration["JWT:Issuer"],
                Audience = _configuration["JWT:Audience"],
                SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                    new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                    Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            return Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
        }
    }
}
