using AgOne.Shared.Auth.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgOne.Shared.Auth.Api.Data;

/// <summary>
/// DbContext for AG ONE token storage.
/// 
/// *** INTEGRATION OPTIONS ***
/// 
/// Option A (Recommended): Add the entities to your EXISTING DbContext
///   - Just add the two DbSet properties and the OnModelCreating configuration
///   - See the "COPY TO YOUR EXISTING DBCONTEXT" section below
/// 
/// Option B: Use this as a separate DbContext (isolated token DB)
///   - Register this context alongside your existing one
///   - Uses a separate connection string "AgOneTokenDb"
/// </summary>
public class AgOneTokenDbContext : DbContext
{
    public AgOneTokenDbContext(DbContextOptions<AgOneTokenDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserToken> UserTokens => Set<UserToken>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureAgOneTokenEntities(modelBuilder);
    }

    /// <summary>
    /// *** COPY THIS METHOD TO YOUR EXISTING DBCONTEXT ***
    /// Call it from your OnModelCreating:
    ///   AgOneTokenDbContext.ConfigureAgOneTokenEntities(modelBuilder);
    /// </summary>
    public static void ConfigureAgOneTokenEntities(ModelBuilder modelBuilder)
    {
        // UserToken configuration
        modelBuilder.Entity<UserToken>(entity =>
        {
            entity.ToTable("UserTokens");

            // Unique constraint: one token per user per product
            entity.HasIndex(e => new { e.UserId, e.ProductName })
                .IsUnique()
                .HasDatabaseName("IX_UserTokens_UserId_Product");

            // Index for quick lookups by user
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_UserTokens_UserId");

            // Index for cleanup jobs (find expired tokens)
            entity.HasIndex(e => e.AccessTokenExpiresUtc)
                .HasDatabaseName("IX_UserTokens_Expiry");

            // Index for active tokens
            entity.HasIndex(e => new { e.IsActive, e.UserId })
                .HasDatabaseName("IX_UserTokens_Active_UserId");
        });

        // UserSession configuration
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.ToTable("UserSessions");

            entity.HasIndex(e => e.SessionId)
                .IsUnique()
                .HasDatabaseName("IX_UserSessions_SessionId");

            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_UserSessions_UserId");

            entity.HasIndex(e => new { e.IsActive, e.UserId })
                .HasDatabaseName("IX_UserSessions_Active_UserId");

            entity.HasIndex(e => e.LastActivityUtc)
                .HasDatabaseName("IX_UserSessions_LastActivity");
        });
    }
}
