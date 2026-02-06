using AgOne.Shared.Auth.Api.Data;
using AgOne.Shared.Auth.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgOne.Shared.Auth.Api.Extensions;

/// <summary>
/// Extension methods to register AG ONE token storage services.
/// Add these to your API's Program.cs to enable database token persistence.
/// </summary>
public static class TokenStorageExtensions
{
    /// <summary>
    /// Adds AG ONE token storage services with a dedicated AgOneTokenDbContext.
    /// 
    /// Call in Program.cs:
    ///   builder.Services.AddAgOneTokenStorage(builder.Configuration);
    /// 
    /// Requires a connection string named "AgOneTokenDb" in appsettings.json:
    ///   "ConnectionStrings": {
    ///     "AgOneTokenDb": "Server=...;Database=...;..."
    ///   }
    /// </summary>
    public static IServiceCollection AddAgOneTokenStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register the DbContext
        services.AddDbContext<AgOneTokenDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("AgOneTokenDb");
            if (!string.IsNullOrEmpty(connectionString))
            {
                // Default: SQL Server. Change to your provider as needed.
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsHistoryTable("__AgOneTokenMigrations");
                    sqlOptions.EnableRetryOnFailure(3);
                });
            }
            else
            {
                // Fallback to in-memory for development/testing
                options.UseInMemoryDatabase("AgOneTokens");
            }
        });

        // Register the token storage service
        services.AddScoped<ITokenStorageService, TokenStorageService>();

        // Register a named HttpClient for calling Entra ID token endpoint
        services.AddHttpClient("AgOne.EntraIdTokenEndpoint", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }

    /// <summary>
    /// Adds AG ONE token storage services using YOUR EXISTING DbContext.
    /// Use this if you've added UserToken and UserSession entities to your own context.
    /// 
    /// Call in Program.cs:
    ///   builder.Services.AddAgOneTokenStorage<YourDbContext>(builder.Configuration);
    /// 
    /// Prerequisites:
    ///   1. Add DbSet<UserToken> and DbSet<UserSession> to your DbContext
    ///   2. Call AgOneTokenDbContext.ConfigureAgOneTokenEntities(modelBuilder) in OnModelCreating
    ///   3. Add a migration for the new tables
    /// </summary>
    public static IServiceCollection AddAgOneTokenStorage<TContext>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TContext : DbContext
    {
        // When using an existing context, we register a factory that wraps it
        // This allows TokenStorageService to use the AgOneTokenDbContext interface
        // while the actual storage goes through your existing context
        services.AddScoped<AgOneTokenDbContext>(sp =>
        {
            var existingContext = sp.GetRequiredService<TContext>();
            // Create a wrapper that uses the same underlying provider
            var optionsBuilder = new DbContextOptionsBuilder<AgOneTokenDbContext>();
            optionsBuilder.UseSqlServer(
                existingContext.Database.GetConnectionString() ?? "");
            return new AgOneTokenDbContext(optionsBuilder.Options);
        });

        // Register the token storage service
        services.AddScoped<ITokenStorageService, TokenStorageService>();

        // Register a named HttpClient for calling Entra ID token endpoint
        services.AddHttpClient("AgOne.EntraIdTokenEndpoint", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }

    /// <summary>
    /// Adds the background service that cleans up expired tokens and sessions.
    /// Optional but recommended for production.
    /// 
    /// Call in Program.cs:
    ///   builder.Services.AddAgOneTokenCleanup();
    /// </summary>
    public static IServiceCollection AddAgOneTokenCleanup(
        this IServiceCollection services)
    {
        services.AddHostedService<TokenCleanupBackgroundService>();
        return services;
    }
}
