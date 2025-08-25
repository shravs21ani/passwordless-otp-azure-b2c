using PasswordlessOTP.API.Models;

namespace PasswordlessOTP.API.Services;

public interface IOTPService
{
    /// <summary>
    /// Generates and sends OTP to the specified user
    /// </summary>
    Task<OTPGenerationResult> GenerateOTPAsync(string identifier, OTPDeliveryMethod deliveryMethod);
    
    /// <summary>
    /// Validates the provided OTP code
    /// </summary>
    Task<OTPValidationResult> ValidateOTPAsync(string identifier, string otpCode);
    
    /// <summary>
    /// Resends OTP with retry logic (30s → 1m → 1.5m)
    /// </summary>
    Task<OTPGenerationResult> ResendOTPAsync(string identifier, OTPDeliveryMethod deliveryMethod);
    
    /// <summary>
    /// Cancels an active OTP request
    /// </summary>
    Task<bool> CancelOTPAsync(string identifier);
    
    /// <summary>
    /// Gets the current OTP status for a user
    /// </summary>
    Task<OTPStatusResult> GetOTPStatusAsync(string identifier);
}

public class OTPGenerationResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? OTPCode { get; set; } // Only for development/testing
    public DateTime ExpiresAt { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public OTPDeliveryMethod DeliveryMethod { get; set; }
}

public class OTPValidationResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public User? User { get; set; }
}

public class OTPStatusResult
{
    public bool HasActiveOTP { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public bool IsBlocked { get; set; }
    public DateTime? BlockedUntil { get; set; }
}

