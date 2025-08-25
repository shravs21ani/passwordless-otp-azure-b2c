using PasswordlessOTP.API.Models;

namespace PasswordlessOTP.API.Services;

public interface INotificationService
{
    /// <summary>
    /// Sends OTP via the specified delivery method
    /// </summary>
    Task<bool> SendOTPAsync(OTPDeliveryMethod deliveryMethod, string recipient, string otpCode, string userName);
    
    /// <summary>
    /// Sends welcome message after successful registration
    /// </summary>
    Task<bool> SendWelcomeMessageAsync(OTPDeliveryMethod deliveryMethod, string recipient, string userName);
    
    /// <summary>
    /// Sends security alert for suspicious activity
    /// </summary>
    Task<bool> SendSecurityAlertAsync(OTPDeliveryMethod deliveryMethod, string recipient, string userName, string alertType);
}

