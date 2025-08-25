using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PasswordlessOTP.API.Data;
using PasswordlessOTP.API.Models;
using System.Security.Cryptography;

namespace PasswordlessOTP.API.Services;

public class OTPService : IOTPService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OTPService> _logger;

    public OTPService(
        ApplicationDbContext context,
        INotificationService notificationService,
        IConfiguration configuration,
        ILogger<OTPService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<OTPGenerationResult> GenerateOTPAsync(string identifier, OTPDeliveryMethod deliveryMethod)
    {
        try
        {
            // Find user by email or phone
            var user = await FindUserByIdentifierAsync(identifier);
            if (user == null)
            {
                return new OTPGenerationResult
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            // Check if user is blocked
            if (user.IsOTPBlocked)
            {
                return new OTPGenerationResult
                {
                    Success = false,
                    Message = $"Account is temporarily blocked. Try again after {user.OTPBlockedUntil:HH:mm:ss}"
                };
            }

            // Cancel any existing OTP requests
            await CancelExistingOTPRequestsAsync(user.Id);

            // Generate new OTP
            var otpCode = GenerateOTPCode();
            var expiryMinutes = _configuration.GetValue<int>("OTP:ExpiryMinutes", 5);
            var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

            var otpRequest = new OTPRequest
            {
                UserId = user.Id,
                OTPCode = otpCode,
                ExpiresAt = expiresAt,
                DeliveryMethod = deliveryMethod,
                DeliveryDetails = deliveryMethod == OTPDeliveryMethod.SMS ? user.PhoneNumber : user.Email,
                Status = OTPStatus.Pending,
                RetryCount = 0
            };

            _context.OTPRequests.Add(otpRequest);
            await _context.SaveChangesAsync();

            // Send OTP via notification service
            var sent = await _notificationService.SendOTPAsync(
                deliveryMethod,
                otpRequest.DeliveryDetails!,
                otpCode,
                user.FirstName);

            if (!sent)
            {
                _logger.LogError("Failed to send OTP to {Identifier}", identifier);
                return new OTPGenerationResult
                {
                    Success = false,
                    Message = "Failed to send OTP. Please try again."
                };
            }

            // Update user's last OTP request time
            user.LastOTPRequestAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("OTP generated successfully for user {UserId}", user.Id);

            return new OTPGenerationResult
            {
                Success = true,
                Message = $"OTP sent to your {deliveryMethod.ToString().ToLower()}",
                OTPCode = _configuration["ASPNETCORE_ENVIRONMENT"] == "Development" ? otpCode : null,
                ExpiresAt = expiresAt,
                RetryCount = 0,
                DeliveryMethod = deliveryMethod
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating OTP for {Identifier}", identifier);
            return new OTPGenerationResult
            {
                Success = false,
                Message = "An error occurred while generating OTP"
            };
        }
    }

    public async Task<OTPValidationResult> ValidateOTPAsync(string identifier, string otpCode)
    {
        try
        {
            var user = await FindUserByIdentifierAsync(identifier);
            if (user == null)
            {
                return new OTPValidationResult
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            // Find active OTP request
            var otpRequest = await _context.OTPRequests
                .Where(o => o.UserId == user.Id && 
                           o.Status == OTPStatus.Pending && 
                           !o.IsExpired)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpRequest == null)
            {
                return new OTPValidationResult
                {
                    Success = false,
                    Message = "No active OTP found. Please request a new one."
                };
            }

            // Check if max attempts reached
            if (otpRequest.IsMaxAttemptsReached)
            {
                otpRequest.Status = OTPStatus.MaxAttemptsReached;
                await _context.SaveChangesAsync();

                // Block user temporarily
                user.OTPBlockedUntil = DateTime.UtcNow.AddMinutes(15);
                user.OTPAttempts = 0;
                await _context.SaveChangesAsync();

                return new OTPValidationResult
                {
                    Success = false,
                    Message = "Maximum OTP attempts reached. Account blocked for 15 minutes."
                };
            }

            // Validate OTP code
            if (otpRequest.OTPCode != otpCode)
            {
                otpRequest.Attempts++;
                await _context.SaveChangesAsync();

                var remainingAttempts = otpRequest.MaxAttempts - otpRequest.Attempts;
                return new OTPValidationResult
                {
                    Success = false,
                    Message = $"Invalid OTP. {remainingAttempts} attempts remaining."
                };
            }

            // OTP is valid - mark as verified
            otpRequest.Status = OTPStatus.Verified;
            otpRequest.VerifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Reset user's OTP attempts
            user.OTPAttempts = 0;
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Generate JWT tokens (simplified for demo)
            var accessToken = GenerateJWTToken(user);
            var refreshToken = GenerateRefreshToken();

            // Create user session
            var userSession = new UserSession
            {
                UserId = user.Id,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                LastUsedAt = DateTime.UtcNow
            };

            _context.UserSessions.Add(userSession);
            await _context.SaveChangesAsync();

            _logger.LogInformation("OTP validated successfully for user {UserId}", user.Id);

            return new OTPValidationResult
            {
                Success = true,
                Message = "OTP validated successfully",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = userSession.ExpiresAt,
                User = user
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating OTP for {Identifier}", identifier);
            return new OTPValidationResult
            {
                Success = false,
                Message = "An error occurred while validating OTP"
            };
        }
    }

    public async Task<OTPGenerationResult> ResendOTPAsync(string identifier, OTPDeliveryMethod deliveryMethod)
    {
        try
        {
            var user = await FindUserByIdentifierAsync(identifier);
            if (user == null)
            {
                return new OTPGenerationResult
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            // Check if user is blocked
            if (user.IsOTPBlocked)
            {
                return new OTPGenerationResult
                {
                    Success = false,
                    Message = $"Account is temporarily blocked. Try again after {user.OTPBlockedUntil:HH:mm:ss}"
                };
            }

            // Find existing OTP request
            var existingOTP = await _context.OTPRequests
                .Where(o => o.UserId == user.Id && 
                           o.Status == OTPStatus.Pending && 
                           !o.IsExpired)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (existingOTP == null)
            {
                return new OTPGenerationResult
                {
                    Success = false,
                    Message = "No active OTP found. Please request a new one."
                };
            }

            // Check retry logic (30s → 1m → 1.5m)
            var retryIntervals = _configuration.GetSection("OTP:RetryIntervals").Get<int[]>() ?? new[] { 30, 60, 90 };
            var maxRetries = _configuration.GetValue<int>("OTP:MaxRetries", 3);

            if (existingOTP.RetryCount >= maxRetries)
            {
                return new OTPGenerationResult
                {
                    Success = false,
                    Message = "Maximum retry attempts reached. Please request a new OTP."
                };
            }

            // Calculate next retry time
            var nextRetrySeconds = retryIntervals[existingOTP.RetryCount];
            var nextRetryAt = DateTime.UtcNow.AddSeconds(nextRetrySeconds);

            // Update retry count and next retry time
            existingOTP.RetryCount++;
            existingOTP.NextRetryAt = nextRetryAt;
            await _context.SaveChangesAsync();

            // Send new OTP
            var otpCode = GenerateOTPCode();
            var expiryMinutes = _configuration.GetValue<int>("OTP:ExpiryMinutes", 5);
            var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

            // Update existing OTP with new code
            existingOTP.OTPCode = otpCode;
            existingOTP.ExpiresAt = expiresAt;
            existingOTP.CreatedAt = DateTime.UtcNow;
            existingOTP.Attempts = 0;
            await _context.SaveChangesAsync();

            // Send OTP via notification service
            var sent = await _notificationService.SendOTPAsync(
                deliveryMethod,
                existingOTP.DeliveryDetails!,
                otpCode,
                user.FirstName);

            if (!sent)
            {
                return new OTPGenerationResult
                {
                    Success = false,
                    Message = "Failed to send OTP. Please try again."
                };
            }

            _logger.LogInformation("OTP resent successfully for user {UserId}, retry {RetryCount}", 
                user.Id, existingOTP.RetryCount);

            return new OTPGenerationResult
            {
                Success = true,
                Message = $"OTP resent to your {deliveryMethod.ToString().ToLower()}",
                OTPCode = _configuration["ASPNETCORE_ENVIRONMENT"] == "Development" ? otpCode : null,
                ExpiresAt = expiresAt,
                RetryCount = existingOTP.RetryCount,
                NextRetryAt = nextRetryAt,
                DeliveryMethod = deliveryMethod
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending OTP for {Identifier}", identifier);
            return new OTPGenerationResult
            {
                Success = false,
                Message = "An error occurred while resending OTP"
            };
        }
    }

    public async Task<bool> CancelOTPAsync(string identifier)
    {
        try
        {
            var user = await FindUserByIdentifierAsync(identifier);
            if (user == null) return false;

            var activeOTPs = await _context.OTPRequests
                .Where(o => o.UserId == user.Id && 
                           o.Status == OTPStatus.Pending && 
                           !o.IsExpired)
                .ToListAsync();

            foreach (var otp in activeOTPs)
            {
                otp.Status = OTPStatus.Cancelled;
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling OTP for {Identifier}", identifier);
            return false;
        }
    }

    public async Task<OTPStatusResult> GetOTPStatusAsync(string identifier)
    {
        try
        {
            var user = await FindUserByIdentifierAsync(identifier);
            if (user == null)
            {
                return new OTPStatusResult
                {
                    HasActiveOTP = false
                };
            }

            var activeOTP = await _context.OTPRequests
                .Where(o => o.UserId == user.Id && 
                           o.Status == OTPStatus.Pending && 
                           !o.IsExpired)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            return new OTPStatusResult
            {
                HasActiveOTP = activeOTP != null,
                ExpiresAt = activeOTP?.ExpiresAt,
                RetryCount = activeOTP?.RetryCount ?? 0,
                NextRetryAt = activeOTP?.NextRetryAt,
                IsBlocked = user.IsOTPBlocked,
                BlockedUntil = user.OTPBlockedUntil
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting OTP status for {Identifier}", identifier);
            return new OTPStatusResult
            {
                HasActiveOTP = false
            };
        }
    }

    private async Task<User?> FindUserByIdentifierAsync(string identifier)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => 
                u.Email == identifier || 
                u.PhoneNumber == identifier);
    }

    private async Task CancelExistingOTPRequestsAsync(Guid userId)
    {
        var existingOTPs = await _context.OTPRequests
            .Where(o => o.UserId == userId && 
                       o.Status == OTPStatus.Pending && 
                       !o.IsExpired)
            .ToListAsync();

        foreach (var otp in existingOTPs)
        {
            otp.Status = OTPStatus.Cancelled;
        }

        await _context.SaveChangesAsync();
    }

    private string GenerateOTPCode()
    {
        var length = _configuration.GetValue<int>("OTP:Length", 6);
        var allowedChars = _configuration.GetValue<string>("OTP:AllowedCharacters", "0123456789");
        
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);
        
        var otp = new char[length];
        for (int i = 0; i < length; i++)
        {
            otp[i] = allowedChars[bytes[i] % allowedChars.Length];
        }
        
        return new string(otp);
    }

    private string GenerateJWTToken(User user)
    {
        // Simplified JWT generation for demo
        // In production, use proper JWT library
        var payload = $"{{\"sub\":\"{user.Id}\",\"email\":\"{user.Email}\",\"exp\":{DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()}}}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload));
    }

    private string GenerateRefreshToken()
    {
        return Guid.NewGuid().ToString("N");
    }
}

