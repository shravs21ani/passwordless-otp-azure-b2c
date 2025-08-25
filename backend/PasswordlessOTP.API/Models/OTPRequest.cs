using System.ComponentModel.DataAnnotations;

namespace PasswordlessOTP.API.Models;

public class OTPRequest
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    [StringLength(10)]
    public string OTPCode { get; set; } = string.Empty;
    
    [Required]
    public DateTime ExpiresAt { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? VerifiedAt { get; set; }
    
    public DateTime? ExpiredAt { get; set; }
    
    public int Attempts { get; set; } = 0;
    
    public int MaxAttempts { get; set; } = 3;
    
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    
    public bool IsVerified => VerifiedAt.HasValue;
    
    public bool IsMaxAttemptsReached => Attempts >= MaxAttempts;
    
    public OTPDeliveryMethod DeliveryMethod { get; set; }
    
    public OTPStatus Status { get; set; } = OTPStatus.Pending;
    
    public string? DeliveryDetails { get; set; } // Phone number or email used for delivery
    
    public int RetryCount { get; set; } = 0;
    
    public DateTime? NextRetryAt { get; set; }
    
    // Navigation property
    public virtual User User { get; set; } = null!;
}

public enum OTPDeliveryMethod
{
    SMS,
    Email
}

public enum OTPStatus
{
    Pending,
    Verified,
    Expired,
    MaxAttemptsReached,
    Cancelled
}

