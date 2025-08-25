using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PasswordlessOTP.API.Models;

public class User
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;
    
    [StringLength(20)]
    public string? PhoneNumber { get; set; }
    
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [StringLength(50)]
    public string? AzureB2CObjectId { get; set; }
    
    [StringLength(50)]
    public string? OktaUserId { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastLoginAt { get; set; }
    
    public DateTime? LastOTPRequestAt { get; set; }
    
    public int OTPAttempts { get; set; } = 0;
    
    public DateTime? OTPBlockedUntil { get; set; }
    
    // Navigation properties
    public virtual ICollection<OTPRequest> OTPRequests { get; set; } = new List<OTPRequest>();
    public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
    
    // Computed properties
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();
    
    [NotMapped]
    public bool IsOTPBlocked => OTPBlockedUntil.HasValue && OTPBlockedUntil.Value > DateTime.UtcNow;
}

