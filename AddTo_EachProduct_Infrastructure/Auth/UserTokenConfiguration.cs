using AgOne.Infrastructure.Auth.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgOne.Infrastructure.Auth;

/// <summary>
/// EF Core Fluent API configuration for UserToken entity.
/// </summary>
public class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
{
    public void Configure(EntityTypeBuilder<UserToken> builder)
    {
        builder.ToTable("UserTokens");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .UseIdentityColumn();

        builder.Property(e => e.UserId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(e => e.Email)
            .HasMaxLength(256);

        builder.Property(e => e.DisplayName)
            .HasMaxLength(256);

        builder.Property(e => e.ProductName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.AccessToken)
            .IsRequired();

        builder.Property(e => e.GrantedScopes)
            .HasMaxLength(1024);

        builder.Property(e => e.TenantId)
            .HasMaxLength(128);

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedUtc)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.UpdatedUtc)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.LastIpAddress)
            .HasMaxLength(45);

        builder.Property(e => e.LastUserAgent)
            .HasMaxLength(512);

        builder.Property(e => e.RefreshCount)
            .IsRequired()
            .HasDefaultValue(0);

        // Indexes
        builder.HasIndex(e => new { e.UserId, e.ProductName })
            .IsUnique()
            .HasDatabaseName("IX_UserTokens_UserId_Product");

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_UserTokens_UserId");

        builder.HasIndex(e => e.AccessTokenExpiresUtc)
            .HasDatabaseName("IX_UserTokens_Expiry");

        builder.HasIndex(e => new { e.IsActive, e.UserId })
            .HasDatabaseName("IX_UserTokens_Active_UserId");
    }
}
