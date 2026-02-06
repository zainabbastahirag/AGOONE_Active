namespace AgOne.Shared.Auth.Configuration;

/// <summary>
/// SSO configuration settings shared across all AG ONE products.
/// Maps to the "AgOneSso" section in appsettings.json.
/// </summary>
public class AgOneSsoSettings
{
    public const string SectionName = "AgOneSso";

    /// <summary>
    /// Azure Entra ID authority URL.
    /// Format: https://{tenant}.ciamlogin.com/ (for External ID)
    /// or https://{tenant}.b2clogin.com/{tenant}.onmicrosoft.com/{policyName} (for B2C)
    /// or https://login.microsoftonline.com/{tenantId} (for Entra ID)
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// The Application (client) ID registered in Azure Entra ID for this product.
    /// Each product can have its own App Registration or share one.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// The scopes to request during authentication.
    /// Should include the API scope for your backend.
    /// Example: ["api://{api-client-id}/access_as_user", "openid", "profile", "email"]
    /// </summary>
    public List<string> DefaultScopes { get; set; } = new();

    /// <summary>
    /// Additional scopes needed for cross-product API calls.
    /// </summary>
    public List<string> AdditionalScopes { get; set; } = new();

    /// <summary>
    /// The post-logout redirect URI.
    /// </summary>
    public string PostLogoutRedirectUri { get; set; } = "/";

    /// <summary>
    /// The redirect URI after successful login (relative path).
    /// </summary>
    public string LoginCallbackPath { get; set; } = "/authentication/login-callback";

    /// <summary>
    /// The redirect URI after logout (relative path).
    /// </summary>
    public string LogoutCallbackPath { get; set; } = "/authentication/logout-callback";

    /// <summary>
    /// The AG ONE Portal base URL. Used for redirecting unauthenticated
    /// users back to the main portal for login.
    /// Example: https://portal.agone.com
    /// </summary>
    public string AgOnePortalUrl { get; set; } = string.Empty;

    /// <summary>
    /// Current product identifier (Learn, Safe, Work, Pulse, Portal).
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Whether to redirect unauthenticated users to AG ONE Portal
    /// instead of showing a local login page.
    /// Set to true for all products except the Portal itself.
    /// </summary>
    public bool RedirectToPortalOnUnauthenticated { get; set; } = true;

    /// <summary>
    /// Known product URLs for cross-product navigation.
    /// </summary>
    public ProductUrls Products { get; set; } = new();

    /// <summary>
    /// The backend API base URL for this product.
    /// </summary>
    public string ApiBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// The sign-up/sign-in policy name (user flow) in Entra External ID / B2C.
    /// Example: "B2C_1_SignUpSignIn"
    /// </summary>
    public string SignUpSignInPolicyId { get; set; } = string.Empty;

    /// <summary>
    /// Token cache duration in minutes. Default 60.
    /// </summary>
    public int TokenCacheMinutes { get; set; } = 60;

    /// <summary>
    /// Enable MSAL logging for debugging. Disable in production.
    /// </summary>
    public bool EnableMsalLogging { get; set; } = false;
}

/// <summary>
/// URLs for all AG ONE products. Used for cross-product navigation and validation.
/// </summary>
public class ProductUrls
{
    public string Portal { get; set; } = string.Empty;
    public string Learn { get; set; } = string.Empty;
    public string Safe { get; set; } = string.Empty;
    public string Work { get; set; } = string.Empty;
    public string Pulse { get; set; } = string.Empty;

    /// <summary>
    /// Returns all product URLs as a list (useful for CORS configuration).
    /// </summary>
    public IEnumerable<string> GetAllUrls()
    {
        var urls = new List<string>();
        if (!string.IsNullOrEmpty(Portal)) urls.Add(Portal);
        if (!string.IsNullOrEmpty(Learn)) urls.Add(Learn);
        if (!string.IsNullOrEmpty(Safe)) urls.Add(Safe);
        if (!string.IsNullOrEmpty(Work)) urls.Add(Work);
        if (!string.IsNullOrEmpty(Pulse)) urls.Add(Pulse);
        return urls;
    }
}
