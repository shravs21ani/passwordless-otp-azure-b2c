using Microsoft.EntityFrameworkCore;
using PasswordlessOTP.API.Models;

namespace PasswordlessOTP.API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<OTPRequest> OTPRequests { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.PhoneNumber).IsUnique();
            entity.HasIndex(e => e.AzureB2CObjectId).IsUnique();
            entity.HasIndex(e => e.OktaUserId).IsUnique();
            
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
        });

        // OTPRequest configuration
        modelBuilder.Entity<OTPRequest>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ExpiresAt);
            
            entity.Property(e => e.OTPCode).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.DeliveryMethod).HasConversion<string>();
            
            entity.HasOne(e => e.User)
                  .WithMany(u => u.OTPRequests)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // UserSession configuration
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.AccessToken).IsUnique();
            
            entity.Property(e => e.AccessToken).IsRequired().HasMaxLength(500);
            entity.Property(e => e.RefreshToken).HasMaxLength(500);
            
            entity.HasOne(e => e.User)
                  .WithMany(u => u.UserSessions)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed data for development
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            SeedDevelopmentData(modelBuilder);
        }
    }

    private void SeedDevelopmentData(ModelBuilder modelBuilder)
    {
        var testUserId = Guid.NewGuid();
        
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = testUserId,
            Email = "test@example.com",
            PhoneNumber = "+1234567890",
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
    }
}

