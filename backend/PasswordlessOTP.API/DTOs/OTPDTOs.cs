using System.ComponentModel.DataAnnotations;
using PasswordlessOTP.API.Models;

namespace PasswordlessOTP.API.DTOs;

public class GenerateOTPRequest
{
    [Required]
    [EmailAddress]
    public string Identifier { get; set; } = string.Empty;
    
    [Required]
    public OTPDeliveryMethod DeliveryMethod { get; set; }
}

public class ValidateOTPRequest
{
    [Required]
    [EmailAddress]
    public string Identifier { get; set; } = string.Empty;
    
    [Required]
    [StringLength(10, MinimumLength = 4)]
    public string OTPCode { get; set; } = string.Empty;
}

public class ResendOTPRequest
{
    [Required]
    [EmailAddress]
    public string Identifier { get; set; } = string.Empty;
    
    [Required]
    public OTPDeliveryMethod DeliveryMethod { get; set; }
}

public class CancelOTPRequest
{
    [Required]
    [EmailAddress]
    public string Identifier { get; set; } = string.Empty;
}

public class UserRegistrationRequest
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Phone]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }
}

public class UserResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string FullName { get; set; } = string.Empty;
}

public class AuthResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public UserResponse? User { get; set; }
}

