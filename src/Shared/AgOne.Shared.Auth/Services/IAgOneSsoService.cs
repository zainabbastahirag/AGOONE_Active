namespace AgOne.Shared.Auth.Services;

/// <summary>
/// Service interface for AG ONE SSO operations.
/// Provides methods for cross-product authentication and navigation.
/// </summary>
public interface IAgOneSsoService
{
    /// <summary>
    /// Checks if the current user has an active SSO session.
    /// </summary>
    Task<bool> IsAuthenticatedAsync();

    /// <summary>
    /// Gets the current user's display name.
    /// </summary>
    Task<string?> GetUserDisplayNameAsync();

    /// <summary>
    /// Gets the current user's email address.
    /// </summary>
    Task<string?> GetUserEmailAsync();

    /// <summary>
    /// Gets the current user's Object ID (OID) from Entra ID.
    /// </summary>
    Task<string?> GetUserIdAsync();

    /// <summary>
    /// Gets the current access token for the product's API.
    /// </summary>
    Task<string?> GetAccessTokenAsync();

    /// <summary>
    /// Gets an access token for a specific set of scopes (cross-product calls).
    /// </summary>
    Task<string?> GetAccessTokenForScopesAsync(IEnumerable<string> scopes);

    /// <summary>
    /// Navigates to another AG ONE product. The SSO session will be
    /// used to silently authenticate the user in the target product.
    /// </summary>
    /// <param name="productName">Product name: Learn, Safe, Work, Pulse, Portal</param>
    /// <param name="returnPath">Optional path within the target product.</param>
    void NavigateToProduct(string productName, string? returnPath = null);

    /// <summary>
    /// Initiates the login flow. For products (not Portal), this redirects
    /// to the AG ONE Portal with a return URL.
    /// </summary>
    /// <param name="returnUrl">The URL to return to after login.</param>
    void Login(string? returnUrl = null);

    /// <summary>
    /// Initiates the logout flow. Clears the SSO session across all products.
    /// </summary>
    void Logout();

    /// <summary>
    /// Attempts a silent SSO login. Returns true if successful.
    /// This uses the existing Entra ID session to acquire tokens without user interaction.
    /// </summary>
    Task<bool> TrySilentLoginAsync();
}
