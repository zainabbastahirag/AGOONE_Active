namespace AgOne.Shared.Auth;

/// <summary>
/// API-side auth settings. Maps to "AzureAd" + "AgOneSso" sections in appsettings.json.
/// </summary>
public class AgOneApiAuthSettings
{
    public const string AzureAdSection = "AzureAd";
    public const string AgOneSsoSection = "AgOneSso";

    public string Instance { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SignUpSignInPolicyId { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public List<string> AllowedOrigins { get; set; } = new();
    public bool ValidateIssuer { get; set; } = true;
    public List<string> ValidIssuers { get; set; } = new();
}
