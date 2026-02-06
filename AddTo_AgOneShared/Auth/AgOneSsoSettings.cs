namespace AgOne.Shared.Auth;

/// <summary>
/// SSO configuration shared across all AG ONE products.
/// Maps to "AgOneSso" section in appsettings.json.
/// </summary>
public class AgOneSsoSettings
{
    public const string SectionName = "AgOneSso";

    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public List<string> DefaultScopes { get; set; } = new();
    public List<string> AdditionalScopes { get; set; } = new();
    public string PostLogoutRedirectUri { get; set; } = "/";
    public string LoginCallbackPath { get; set; } = "/authentication/login-callback";
    public string LogoutCallbackPath { get; set; } = "/authentication/logout-callback";
    public string AgOnePortalUrl { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public bool RedirectToPortalOnUnauthenticated { get; set; } = true;
    public ProductUrls Products { get; set; } = new();
    public string ApiBaseUrl { get; set; } = string.Empty;
    public string SignUpSignInPolicyId { get; set; } = string.Empty;
    public int TokenCacheMinutes { get; set; } = 60;
    public bool EnableMsalLogging { get; set; } = false;
}

public class ProductUrls
{
    public string Portal { get; set; } = string.Empty;
    public string Learn { get; set; } = string.Empty;
    public string Safe { get; set; } = string.Empty;
    public string Work { get; set; } = string.Empty;
    public string Pulse { get; set; } = string.Empty;

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
