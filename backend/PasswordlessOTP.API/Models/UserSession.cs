using System.ComponentModel.DataAnnotations;

namespace PasswordlessOTP.API.Models;

public class UserSession
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    [StringLength(500)]
    public string AccessToken { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? RefreshToken { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    public DateTime ExpiresAt { get; set; }
    
    public DateTime? LastUsedAt { get; set; }
    
    public DateTime? RevokedAt { get; set; }
    
    [StringLength(50)]
    public string? AzureB2CObjectId { get; set; }
    
    [StringLength(50)]
    public string? OktaUserId { get; set; }
    
    [StringLength(100)]
    public string? UserAgent { get; set; }
    
    [StringLength(45)]
    public string? IPAddress { get; set; }
    
    public bool IsActive => !IsExpired && !IsRevoked;
    
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    
    public bool IsRevoked => RevokedAt.HasValue;
    
    // Navigation property
    public virtual User User { get; set; } = null!;
}

