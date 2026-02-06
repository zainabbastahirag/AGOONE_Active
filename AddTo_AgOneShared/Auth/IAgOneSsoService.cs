namespace AgOne.Shared.Auth;

/// <summary>
/// SSO service interface. Implemented in the UI project.
/// </summary>
public interface IAgOneSsoService
{
    Task<bool> IsAuthenticatedAsync();
    Task<string?> GetUserDisplayNameAsync();
    Task<string?> GetUserEmailAsync();
    Task<string?> GetUserIdAsync();
    Task<string?> GetAccessTokenAsync();
    Task<string?> GetAccessTokenForScopesAsync(IEnumerable<string> scopes);
    void NavigateToProduct(string productName, string? returnPath = null);
    void Login(string? returnUrl = null);
    void Logout();
    Task<bool> TrySilentLoginAsync();
}
