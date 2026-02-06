using AgOne.Infrastructure.Auth.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgOne.Infrastructure.Auth;

/// <summary>
/// EF Core Fluent API configuration for UserSession entity.
/// </summary>
public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("UserSessions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .UseIdentityColumn();

        builder.Property(e => e.SessionId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(e => e.UserId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(e => e.Email)
            .HasMaxLength(256);

        builder.Property(e => e.InitiatedByProduct)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.ProductsAccessed)
            .HasMaxLength(512);

        builder.Property(e => e.StartedUtc)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.LastActivityUtc)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.IpAddress)
            .HasMaxLength(45);

        builder.Property(e => e.UserAgent)
            .HasMaxLength(512);

        // Indexes
        builder.HasIndex(e => e.SessionId)
            .IsUnique()
            .HasDatabaseName("IX_UserSessions_SessionId");

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_UserSessions_UserId");

        builder.HasIndex(e => new { e.IsActive, e.UserId })
            .HasDatabaseName("IX_UserSessions_Active_UserId");

        builder.HasIndex(e => e.LastActivityUtc)
            .HasDatabaseName("IX_UserSessions_LastActivity");
    }
}
