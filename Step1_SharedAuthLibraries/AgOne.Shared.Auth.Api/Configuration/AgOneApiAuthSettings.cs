namespace AgOne.Shared.Auth.Api.Configuration;

/// <summary>
/// API-side authentication settings for AG ONE SSO.
/// Maps to the "AzureAd" and "AgOneSso" sections in appsettings.json.
/// </summary>
public class AgOneApiAuthSettings
{
    public const string AzureAdSection = "AzureAd";
    public const string AgOneSsoSection = "AgOneSso";

    /// <summary>
    /// Azure Entra ID / External ID instance URL.
    /// Example: https://login.microsoftonline.com/ (for Entra ID)
    /// or https://{tenant}.ciamlogin.com/ (for External ID)
    /// </summary>
    public string Instance { get; set; } = string.Empty;

    /// <summary>
    /// The Azure AD tenant ID or domain.
    /// Example: "your-tenant-id" or "yourtenant.onmicrosoft.com"
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// The Application (client) ID of the API app registration.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// The API audience. Usually the Application ID URI.
    /// Example: "api://your-api-client-id"
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// The sign-up/sign-in policy name (for B2C / External ID).
    /// Example: "B2C_1_SignUpSignIn"
    /// </summary>
    public string SignUpSignInPolicyId { get; set; } = string.Empty;

    /// <summary>
    /// The domain of the B2C / External ID tenant.
    /// Example: "yourtenant.onmicrosoft.com"
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// Current product name for this API instance.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// CORS allowed origins - all product frontend URLs.
    /// </summary>
    public List<string> AllowedOrigins { get; set; } = new();

    /// <summary>
    /// Whether to validate the issuer. Set to false for multi-tenant scenarios.
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// Known valid issuers (for multi-tenant scenarios).
    /// </summary>
    public List<string> ValidIssuers { get; set; } = new();
}
