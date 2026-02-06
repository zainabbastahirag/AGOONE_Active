# AG ONE SSO - Complete Flow Diagram with Code

> Every step of the SSO flow, from first login to cross-product navigation to
> token refresh to database storage — with the exact code that runs at each point.

---

## THE BIG PICTURE

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                                                                              │
│   USER          BLAZOR WASM           ENTRA ID            .NET API    DB     │
│   (Browser)     (MSAL.js)             (Azure Cloud)       (Backend)          │
│                                                                              │
│     │                │                      │                │          │     │
│     │  1. Visit      │                      │                │          │     │
│     │  portal.com    │                      │                │          │     │
│     │───────────────►│                      │                │          │     │
│     │                │                      │                │          │     │
│     │                │  2. No token found   │                │          │     │
│     │                │  Redirect to login   │                │          │     │
│     │◄───────────────│─────────────────────►│                │          │     │
│     │                │                      │                │          │     │
│     │  3. User enters credentials           │                │          │     │
│     │──────────────────────────────────────►│                │          │     │
│     │                │                      │                │          │     │
│     │                │  4. Tokens returned   │                │          │     │
│     │                │◄─────────────────────│                │          │     │
│     │                │  access_token         │                │          │     │
│     │                │  id_token             │                │          │     │
│     │                │  refresh_token        │                │          │     │
│     │                │                      │                │          │     │
│     │                │  5. Store in          │                │          │     │
│     │                │  localStorage         │                │          │     │
│     │                │                      │                │          │     │
│     │  6. App loads  │                      │                │          │     │
│     │◄───────────────│                      │                │          │     │
│     │                │                      │                │          │     │
│     │  7. User clicks│"Load courses"        │                │          │     │
│     │───────────────►│                      │                │          │     │
│     │                │  8. Attach Bearer token                │          │     │
│     │                │─────────────────────────────────────►│          │     │
│     │                │                      │                │          │     │
│     │                │                      │  9. Validate   │          │     │
│     │                │                      │◄───────────────│          │     │
│     │                │                      │  JWT is valid  │          │     │
│     │                │                      │───────────────►│          │     │
│     │                │                      │                │          │     │
│     │                │                      │                │ 10. Save │     │
│     │                │                      │                │ token    │     │
│     │                │                      │                │─────────►│     │
│     │                │                      │                │          │     │
│     │                │  11. API response     │                │          │     │
│     │◄───────────────│◄─────────────────────────────────────│          │     │
│     │                │                      │                │          │     │
│     │  === 55 minutes later ===             │                │          │     │
│     │                │                      │                │          │     │
│     │                │  12. Token expiring   │                │          │     │
│     │                │  Silent refresh       │                │          │     │
│     │                │  (hidden iframe)      │                │          │     │
│     │                │─────────────────────►│                │          │     │
│     │                │                      │  SSO cookie    │          │     │
│     │                │  13. New tokens       │  still valid   │          │     │
│     │                │◄─────────────────────│                │          │     │
│     │                │                      │                │          │     │
│     │  === User clicks "Go to Learn" ===    │                │          │     │
│     │                │                      │                │          │     │
│     │  14. Navigate to learn.agone.com      │                │          │     │
│     │───────────────►│ (new MSAL instance)  │                │          │     │
│     │                │  15. iframe to        │                │          │     │
│     │                │  Entra ID             │                │          │     │
│     │                │─────────────────────►│                │          │     │
│     │                │                      │  SSO cookie!   │          │     │
│     │                │  16. Tokens for Learn │                │          │     │
│     │                │◄─────────────────────│                │          │     │
│     │                │                      │                │          │     │
│     │  17. Learn app loads (no login page!) │                │          │     │
│     │◄───────────────│                      │                │          │     │
│     │                │                      │                │          │     │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## FLOW 1: FIRST LOGIN (AG ONE Portal)

### Step 1 — User visits portal.agone.com

The browser loads the Blazor WASM app. `App.razor` wraps everything in `AuthorizeRouteView`:

```razor
<!-- Your App.razor -->
<CascadingAuthenticationState>
    <Router AppAssembly="@typeof(App).Assembly">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)">
                <NotAuthorized>
                    @if (context.User.Identity?.IsAuthenticated != true)
                    {
                        <!-- THIS TRIGGERS — user is not logged in -->
                        <AgOneRedirectToLogin />
                    }
                </NotAuthorized>
            </AuthorizeRouteView>
        </Found>
    </Router>
</CascadingAuthenticationState>
```

### Step 2 — AgOneRedirectToLogin fires

The component calls `AgOneSsoService.Login()`:

```csharp
// AgOneRedirectToLogin.razor — fires immediately
protected override void OnInitialized()
{
    SsoService.Login(Navigation.Uri);
}

// AgOneSsoService.Login() — for Portal, goes directly to MSAL
public void Login(string? returnUrl = null)
{
    // Portal has RedirectToPortalOnUnauthenticated = false
    // So it uses MSAL directly:
    _navigation.NavigateTo("authentication/login", forceLoad: true);
}
```

### Step 3 — MSAL redirects to Entra ID

The `Authentication.razor` page handles the MSAL redirect:

```razor
<!-- Pages/Authentication.razor -->
@page "/authentication/{action}"
<RemoteAuthenticatorView Action="@Action" />
@code {
    [Parameter] public string? Action { get; set; }
}
```

MSAL.js (configured in `Program.cs`) builds the authorization URL and redirects the browser:

```
REDIRECT TO:
https://YOUR-TENANT.ciamlogin.com/YOUR-TENANT-ID/B2C_1_SignUpSignIn/oauth2/v2.0/authorize
  ?client_id=YOUR-CLIENT-ID
  &redirect_uri=https://portal.agone.com/authentication/login-callback
  &response_type=code
  &scope=openid profile email api://YOUR-API/access_as_user
  &prompt=login
```

This configuration came from your `Program.cs`:

```csharp
// Program.cs — this one line configures everything
builder.Services.AddAgOneSso(builder.Configuration);

// Which internally does:
services.AddMsalAuthentication(options =>
{
    options.ProviderOptions.Authentication.Authority = "https://YOUR-TENANT.ciamlogin.com/";
    options.ProviderOptions.Authentication.ClientId = "YOUR-CLIENT-ID";
    options.ProviderOptions.DefaultAccessTokenScopes.Add("api://YOUR-API/access_as_user");
    options.ProviderOptions.LoginMode = "redirect";
    options.ProviderOptions.Cache.CacheLocation = "localStorage";
});
```

### Step 4 — User enters credentials, Entra ID returns tokens

User logs in at the Entra ID page. Entra ID redirects back to:
```
https://portal.agone.com/authentication/login-callback
  ?code=AUTHORIZATION_CODE_HERE
```

MSAL.js intercepts this, exchanges the code for tokens:

```
POST https://YOUR-TENANT.ciamlogin.com/YOUR-TENANT-ID/B2C_1_SignUpSignIn/oauth2/v2.0/token
Content-Type: application/x-www-form-urlencoded

grant_type=authorization_code
&client_id=YOUR-CLIENT-ID
&code=AUTHORIZATION_CODE_HERE
&redirect_uri=https://portal.agone.com/authentication/login-callback
&scope=openid profile email api://YOUR-API/access_as_user
```

Entra ID responds with:

```json
{
  "access_token": "eyJ0eXAiOiJKV1QiLC...",     // JWT, expires in ~1 hour
  "id_token": "eyJ0eXAiOiJKV1QiLCJhb...",      // JWT with user info
  "refresh_token": "0.ARoAv4j5cvGGr0G...",      // Opaque, expires in 24h-90d
  "token_type": "Bearer",
  "expires_in": 3600,
  "scope": "openid profile email api://YOUR-API/access_as_user"
}
```

### Step 5 — MSAL stores tokens in localStorage

MSAL.js automatically saves all three tokens in the browser's `localStorage`:

```
localStorage keys:
  msal.{clientId}.idtoken          → id_token JWT
  msal.{clientId}.accesstoken      → access_token JWT
  msal.{clientId}.refreshtoken     → refresh_token (opaque)
  msal.{clientId}.account          → user account info
```

### Step 6 — App renders, user is authenticated

`AgOneAuthStateProvider` reads the user claims from the token:

```csharp
// AgOneAuthStateProvider.GetAuthenticationStateAsync()
public override async Task<AuthenticationState> GetAuthenticationStateAsync()
{
    var state = await _underlying.GetAuthenticationStateAsync();
    var user = state.User;

    if (user.Identity?.IsAuthenticated == true)
    {
        // Try to get an access token to validate session is active
        var tokenResult = await _tokenProvider.RequestAccessToken(...);

        if (tokenResult.TryGetToken(out var token))
        {
            // Enrich with AG ONE claims
            var enrichedUser = EnrichClaimsPrincipal(user, token);
            return new AuthenticationState(enrichedUser);
        }
    }
    return CreateUnauthenticatedState();
}
```

Now `<AuthorizeView>` components work:

```razor
<!-- In your pages/components -->
<AuthorizeView>
    <Authorized>
        Welcome, @context.User.Identity?.Name!  <!-- Shows "John Doe" -->
    </Authorized>
</AuthorizeView>
```

---

## FLOW 2: API CALL (Token sent to backend)

### Step 7 — User triggers an API call

```razor
<!-- Your page -->
@inject HttpClient Http

@code {
    private List<Course>? courses;

    protected override async Task OnInitializedAsync()
    {
        // HttpClient is pre-configured with AgOneAuthorizationMessageHandler
        courses = await Http.GetFromJsonAsync<List<Course>>("api/courses");
    }
}
```

### Step 8 — AuthorizationMessageHandler attaches the Bearer token

The handler (registered in `AddAgOneSso`) automatically adds the token:

```csharp
// AgOneAuthorizationMessageHandler (extends AuthorizationMessageHandler)
// This happens automatically on every HTTP request:
//
// Before the request is sent:
// 1. MSAL.js is asked for an access token
// 2. If valid token exists → attached as header
// 3. If expired → MSAL silently refreshes first, then attaches
//
// The outgoing HTTP request looks like:
//
// GET https://portal-api.agone.com/api/courses
// Authorization: Bearer eyJ0eXAiOiJKV1QiLC...
// Content-Type: application/json
```

### Step 9 — API validates the JWT

The JWT Bearer middleware (configured by `AddAgOneSsoApiAuthentication`) validates:

```csharp
// This happens automatically in the ASP.NET Core pipeline:
// 1. Extract "Bearer {token}" from Authorization header
// 2. Validate JWT signature against Entra ID's public keys
// 3. Validate issuer, audience, expiry
// 4. Populate HttpContext.User with claims from the token

// Your controller receives the validated user:
[ApiController]
[Route("api/courses")]
[Authorize(Policy = "AgOne.Authenticated")]
public class CoursesController : ControllerBase
{
    [HttpGet]
    public IActionResult GetCourses()
    {
        // User is guaranteed to be authenticated here
        var userId = User.FindFirst("oid")?.Value;    // "abc-123-def"
        var email = User.FindFirst("preferred_username")?.Value;  // "john@company.com"
        var name = User.FindFirst("name")?.Value;     // "John Doe"

        return Ok(new { courses = GetCoursesForUser(userId) });
    }
}
```

### Step 10 — TokenCaptureMiddleware saves the token to the database

This middleware runs on every authenticated request, AFTER authentication:

```csharp
// TokenCaptureMiddleware.InvokeAsync() — runs automatically
public async Task InvokeAsync(HttpContext context)
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        // 1. Extract Bearer token from header
        var authHeader = context.Request.Headers.Authorization.ToString();
        var accessToken = authHeader["Bearer ".Length..].Trim();

        // 2. Get user info from validated claims
        var userId = context.User.FindFirst("oid")?.Value;     // "abc-123-def"
        var email = context.User.FindFirst("preferred_username")?.Value;

        // 3. Parse JWT to get expiry time
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        var expiresUtc = jwt.ValidTo;  // e.g., 2026-02-06T15:30:00Z

        // 4. Save to database (upsert — creates or updates)
        await tokenStorage.SaveTokenAsync(new SaveTokenRequest
        {
            UserId = userId,                    // "abc-123-def"
            Email = email,                      // "john@company.com"
            ProductName = "Portal",             // From appsettings
            AccessToken = accessToken,          // The full JWT string
            AccessTokenExpiresUtc = expiresUtc, // When it expires
            IpAddress = context.Connection.RemoteIpAddress?.ToString(),
        });

        // 5. Track the session
        await tokenStorage.TrackSessionAsync(new TrackSessionRequest
        {
            UserId = userId,
            ProductName = "Portal",
        });
    }

    await _next(context);  // Continue to controller
}
```

**What the database looks like after this:**

```sql
-- UserTokens table:
-- ┌────┬─────────────┬──────────────────┬─────────┬──────────────────────┬────────────────────────┬──────────┐
-- │ Id │ UserId      │ Email            │ Product │ AccessToken          │ AccessTokenExpiresUtc  │ IsActive │
-- ├────┼─────────────┼──────────────────┼─────────┼──────────────────────┼────────────────────────┼──────────┤
-- │ 1  │ abc-123-def │ john@company.com │ Portal  │ eyJ0eXAiOiJKV1Qi...│ 2026-02-06 15:30:00    │ 1        │
-- └────┴─────────────┴──────────────────┴─────────┴──────────────────────┴────────────────────────┴──────────┘

-- UserSessions table:
-- ┌────┬───────────────────────────────────────┬─────────────┬─────────────────┬──────────────────┐
-- │ Id │ SessionId                             │ UserId      │ ProductsAccessed│ IsActive         │
-- ├────┼───────────────────────────────────────┼─────────────┼─────────────────┼──────────────────┤
-- │ 1  │ f47ac10b-58cc-4372-a567-0e02b2c3d479  │ abc-123-def │ Portal          │ 1                │
-- └────┴───────────────────────────────────────┴─────────────┴─────────────────┴──────────────────┘
```

### Step 11 — Response sent back to browser

```json
// API Response → Blazor client → rendered in UI
{
  "courses": [
    { "id": 1, "name": "Safety Training 101" },
    { "id": 2, "name": "Leadership Skills" }
  ]
}
```

---

## FLOW 3: TOKEN REFRESH (Automatic, ~55 minutes later)

```
TIMELINE:
═══════════════════════════════════════════════════════════════

T=0min    Login. Access token issued (expires in 60 min)
T=5min    API call. Token valid ✅ → request succeeds
T=30min   API call. Token valid ✅ → request succeeds
T=55min   API call. Token EXPIRING ⚠️ → MSAL silently refreshes
T=56min   New token. API call with new token ✅
T=115min  Token expiring again ⚠️ → MSAL refreshes again
...       This repeats as long as the user is active

═══════════════════════════════════════════════════════════════
```

### Step 12 — MSAL detects token is expiring

When your Blazor code makes an API call and the token has < 5 minutes left, the `AuthorizationMessageHandler` asks MSAL for a token, and MSAL automatically tries to refresh:

```
WHAT HAPPENS INSIDE MSAL.js (you don't write this code):

1. Check: Is access_token expired or within 5 min of expiry?
   YES → Need a new token

2. Try SILENT REFRESH via hidden iframe:
   ┌─────────────────────────────────────────────────────┐
   │ Create invisible <iframe> pointing to:               │
   │ https://YOUR-TENANT.ciamlogin.com/.../authorize     │
   │   ?prompt=none          ← Don't show any UI         │
   │   &client_id=...                                     │
   │   &response_type=code                                │
   │   &scope=openid profile api://YOUR-API/access_as_user│
   │                                                       │
   │ The browser sends the SSO session cookie with this    │
   │ request. Entra ID sees the cookie, validates the      │
   │ session, and returns a new authorization code via      │
   │ the iframe's redirect.                                │
   │                                                       │
   │ MSAL exchanges the code for new tokens.               │
   └─────────────────────────────────────────────────────┘

   SUCCESS? → New access_token, update localStorage, done ✅

3. If iframe fails, try REFRESH TOKEN GRANT:
   ┌─────────────────────────────────────────────────────┐
   │ POST https://YOUR-TENANT.ciamlogin.com/.../token    │
   │                                                       │
   │ grant_type=refresh_token                              │
   │ client_id=YOUR-CLIENT-ID                              │
   │ refresh_token=0.ARoAv4j5cvGGr0G...  (from storage)  │
   │ scope=openid profile api://YOUR-API/access_as_user   │
   │                                                       │
   │ Entra ID validates the refresh token and returns:     │
   │ {                                                     │
   │   "access_token": "eyJ...(NEW)...",                  │
   │   "refresh_token": "0.ARo...(NEW)...",  ← rotated    │
   │   "expires_in": 3600                                  │
   │ }                                                     │
   └─────────────────────────────────────────────────────┘

   SUCCESS? → New tokens in localStorage, done ✅

4. If BOTH fail → redirect user to login page
```

### Step 13 — New token used for the API call

The next API call carries the new access token. `TokenCaptureMiddleware` updates the DB:

```sql
-- Database AFTER refresh (same row, updated values):
-- ┌────┬─────────────┬─────────┬──────────────────────────┬────────────────────────┬──────────────┐
-- │ Id │ UserId      │ Product │ AccessToken              │ AccessTokenExpiresUtc  │ RefreshCount │
-- ├────┼─────────────┼─────────┼──────────────────────────┼────────────────────────┼──────────────┤
-- │ 1  │ abc-123-def │ Portal  │ eyJ...(NEW TOKEN)...     │ 2026-02-06 16:30:00    │ 1            │
-- └────┴─────────────┴─────────┴──────────────────────────┴────────────────────────┴──────────────┘
--                                  ▲ Updated!                  ▲ Extended by 1 hour    ▲ Incremented
```

---

## FLOW 4: CROSS-PRODUCT SSO (Portal → Learn)

This is the key SSO flow. User is logged into Portal and navigates to Learn.

### Step 14 — User clicks "Learn" in the product navigator

```razor
<!-- AgOneProductNavigator.razor or your custom nav -->
<a @onclick="() => SsoService.NavigateToProduct("Learn")">Learn</a>
```

```csharp
// AgOneSsoService.NavigateToProduct()
public void NavigateToProduct(string productName, string? returnPath = null)
{
    var productUrl = GetProductUrl(productName);
    // productUrl = "https://learn.agone.com" (from appsettings)

    // Full page navigation to a different domain
    _navigation.NavigateTo("https://learn.agone.com", forceLoad: true);
}
```

### Step 15 — Learn's Blazor WASM loads, MSAL initializes

Learn is a completely separate Blazor WASM app on a different domain. It has its own MSAL instance:

```csharp
// Learn's Program.cs — same single line as Portal
builder.Services.AddAgOneSso(builder.Configuration);
// But Learn's appsettings.json has:
// "ProductName": "Learn"
// "RedirectToPortalOnUnauthenticated": true  ← KEY DIFFERENCE
```

Learn's `App.razor` triggers `AuthorizeRouteView` → user not authenticated → `AgOneAuthGuard` fires:

```csharp
// AgOneAuthGuard.razor.cs
protected override async Task OnInitializedAsync()
{
    if (!await SsoService.IsAuthenticatedAsync())
    {
        _attemptingSilentLogin = true;

        // THIS IS THE SSO MAGIC:
        // Try to get a token WITHOUT showing a login page
        var success = await SsoService.TrySilentLoginAsync();
        // ↓
    }
}

// AgOneSsoService.TrySilentLoginAsync()
public async Task<bool> TrySilentLoginAsync()
{
    var tokenResult = await _tokenProvider.RequestAccessToken(
        new AccessTokenRequestOptions
        {
            Scopes = _settings.DefaultScopes.ToArray()
        });

    // MSAL.js tries to get a token silently:
    // 1. Check localStorage → no token (new domain, empty storage)
    // 2. Try hidden iframe to Entra ID with prompt=none
    //    → Browser sends SSO SESSION COOKIE (set during Portal login!)
    //    → Entra ID recognizes the session
    //    → Returns new tokens for Learn's client_id
    // 3. MSAL stores new Learn tokens in localStorage

    if (tokenResult.TryGetToken(out _))
    {
        return true;  // ✅ SUCCESS — user is silently authenticated!
    }
    return false;
}
```

```
┌───────────────────────────────────────────────────────────────────┐
│                                                                    │
│  WHY THIS WORKS — The SSO Session Cookie                          │
│  ════════════════════════════════════════                          │
│                                                                    │
│  When user logged into Portal, Entra ID set a cookie:             │
│                                                                    │
│  Cookie Domain: YOUR-TENANT.ciamlogin.com                         │
│  Cookie Name:   ESTSSSOTILES (or similar)                         │
│  Cookie Scope:  All apps in the same Entra ID tenant              │
│                                                                    │
│  When Learn's MSAL creates an iframe to:                          │
│  https://YOUR-TENANT.ciamlogin.com/.../authorize?prompt=none      │
│                                                                    │
│  The browser automatically sends this cookie!                      │
│  Entra ID sees it → "Oh, this user already logged in"             │
│  → Issues tokens for Learn WITHOUT showing any login page          │
│                                                                    │
│  THIS is Single Sign-On. One login, access to all products.       │
│                                                                    │
└───────────────────────────────────────────────────────────────────┘
```

### Step 16+17 — Learn app loads, user sees the app

No login page shown! The user is seamlessly authenticated in Learn.

**Database after cross-product navigation:**

```sql
-- UserTokens table now has TWO rows:
-- ┌────┬─────────────┬─────────┬──────────────────────────┬────────────────────────┬──────────┐
-- │ Id │ UserId      │ Product │ AccessToken              │ AccessTokenExpiresUtc  │ IsActive │
-- ├────┼─────────────┼─────────┼──────────────────────────┼────────────────────────┼──────────┤
-- │ 1  │ abc-123-def │ Portal  │ eyJ...(portal token)...  │ 2026-02-06 16:30:00    │ 1        │
-- │ 2  │ abc-123-def │ Learn   │ eyJ...(learn token)...   │ 2026-02-06 15:35:00    │ 1        │
-- └────┴─────────────┴─────────┴──────────────────────────┴────────────────────────┴──────────┘

-- UserSessions table — session updated:
-- ┌────┬───────────────────┬─────────────┬──────────────────┬──────────┐
-- │ Id │ SessionId         │ UserId      │ ProductsAccessed │ IsActive │
-- ├────┼───────────────────┼─────────────┼──────────────────┼──────────┤
-- │ 1  │ f47ac10b-58cc-... │ abc-123-def │ Portal,Learn     │ 1        │
-- └────┴───────────────────┴─────────────┴──────────────────┴──────────┘
--                                           ▲ "Learn" added!
```

---

## FLOW 5: DIRECT URL ACCESS (Blocked without login)

User types `https://learn.agone.com` directly in browser (not logged in).

```
Step 1: Browser loads Learn's Blazor WASM app
Step 2: AuthorizeRouteView → user not authenticated
Step 3: AgOneAuthGuard → TrySilentLoginAsync()
Step 4: MSAL tries iframe to Entra ID with prompt=none
Step 5: NO SSO cookie exists (user never logged in)
Step 6: Entra ID returns error (interaction_required)
Step 7: Silent login FAILS → success = false

Step 8: AgOneSsoService.Login() fires:
```

```csharp
public void Login(string? returnUrl = null)
{
    // Learn has RedirectToPortalOnUnauthenticated = TRUE
    // So we redirect to the Portal, NOT directly to Entra ID:

    var currentUrl = returnUrl ?? _navigation.Uri;
    // currentUrl = "https://learn.agone.com/courses"

    var portalLoginUrl =
        "https://portal.agone.com/authentication/login" +
        "?returnUrl=https%3A%2F%2Flearn.agone.com%2Fcourses";

    // Send user to Portal to log in
    _navigation.NavigateTo(portalLoginUrl, forceLoad: true);
}
```

```
Step 9:  Browser goes to portal.agone.com
Step 10: Portal's MSAL → no token → redirect to Entra ID login
Step 11: User enters credentials
Step 12: Entra ID creates SSO session cookie + returns tokens
Step 13: Portal receives tokens, sees returnUrl parameter
Step 14: Portal redirects to: https://learn.agone.com/courses
Step 15: Learn loads → MSAL iframe → SSO cookie exists! → silent auth ✅
Step 16: User is in Learn, on the /courses page they originally wanted
```

---

## FLOW 6: SERVER-SIDE TOKEN REFRESH (Background Job)

When your API backend needs to call another API on behalf of a user (e.g., a scheduled report generator):

```csharp
// Example: Background job that generates a weekly report for each user
public class WeeklyReportJob
{
    private readonly ITokenStorageService _tokenStorage;
    private readonly IHttpClientFactory _httpClientFactory;

    public async Task GenerateReportForUser(string userId)
    {
        // Step 1: Get a valid access token from the database
        var accessToken = await _tokenStorage.GetValidAccessTokenAsync(
            userId, "Learn");
        //
        // INSIDE GetValidAccessTokenAsync():
        //
        // 1. Query DB: SELECT * FROM UserTokens
        //              WHERE UserId = 'abc-123-def' AND ProductName = 'Learn'
        //
        // 2. Check: Is AccessTokenExpiresUtc > NOW + 5 minutes?
        //    YES → Return the stored access token ✅
        //    NO  → Token expired, need to refresh ↓
        //
        // 3. Refresh: Take RefreshToken from DB row
        //    POST https://YOUR-TENANT.ciamlogin.com/.../token
        //    grant_type=refresh_token
        //    client_id=YOUR-CLIENT-ID
        //    refresh_token=0.ARoAv4j5cvGGr0G... (from DB)
        //
        // 4. Entra ID returns new access_token + new refresh_token
        //
        // 5. UPDATE UserTokens SET
        //      AccessToken = 'eyJ...(NEW)...',
        //      RefreshToken = '0.ARo...(NEW)...',
        //      AccessTokenExpiresUtc = '2026-02-06 17:30:00',
        //      RefreshCount = RefreshCount + 1
        //    WHERE UserId = 'abc-123-def' AND ProductName = 'Learn'
        //
        // 6. Return the new access token

        if (accessToken == null)
        {
            // No valid token AND refresh failed
            // User needs to log in again via browser
            Console.WriteLine($"Cannot generate report for user {userId} — re-auth needed");
            return;
        }

        // Step 2: Use the token to call the Learn API
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var courses = await httpClient.GetFromJsonAsync<List<Course>>(
            "https://learn-api.agone.com/api/courses/user-progress");

        // Step 3: Generate the report
        await GenerateReport(userId, courses);
    }
}
```

The refresh token exchange that happens inside `TokenStorageService`:

```csharp
// TokenStorageService.ExchangeRefreshTokenAsync() — the actual HTTP call
private async Task<TokenRefreshResult> ExchangeRefreshTokenAsync(UserToken storedToken)
{
    // Build the Entra ID token endpoint URL
    var tokenEndpoint =
        "https://YOUR-TENANT.ciamlogin.com/YOUR-TENANT-ID/B2C_1_SignUpSignIn/oauth2/v2.0/token";

    // Build the form-encoded body
    var formData = new Dictionary<string, string>
    {
        ["grant_type"]    = "refresh_token",
        ["client_id"]     = "YOUR-CLIENT-ID",          // From appsettings
        ["refresh_token"] = storedToken.RefreshToken,   // From database
        ["scope"]         = storedToken.GrantedScopes,  // Same scopes as before
    };

    // Send the request
    var response = await httpClient.PostAsync(tokenEndpoint,
        new FormUrlEncodedContent(formData));

    // Parse the response
    // {
    //   "access_token": "eyJ...(new)...",
    //   "refresh_token": "0.ARo...(new, rotated)...",
    //   "expires_in": 3600
    // }

    return TokenRefreshResult.Succeeded(
        accessToken: tokenResponse.AccessToken,
        refreshToken: tokenResponse.RefreshToken,
        expiresUtc: DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn)
    );
}
```

---

## FLOW 7: LOGOUT

```
┌───────────────────────────────────────────────────────────────────┐
│  User clicks "Logout" in ANY product                              │
│                                                                    │
│  BROWSER SIDE:                                                    │
│  ─────────────                                                    │
│  1. AgOneSsoService.Logout() →                                    │
│     MSAL clears localStorage (tokens deleted from browser)        │
│     MSAL redirects to Entra ID logout endpoint:                   │
│     https://YOUR-TENANT.ciamlogin.com/.../logout                  │
│     ?post_logout_redirect_uri=https://portal.agone.com            │
│                                                                    │
│  2. Entra ID DELETES the SSO session cookie                      │
│     (This means ALL products lose SSO — no more silent login)     │
│                                                                    │
│  API SIDE:                                                        │
│  ─────────                                                        │
│  3. Client calls POST /api/agone-auth/logout?allProducts=true     │
│                                                                    │
│  4. TokenStorageService.DeactivateAllUserTokensAsync("abc-123-def")│
│     → UPDATE UserTokens SET IsActive = 0                          │
│       WHERE UserId = 'abc-123-def'                                │
│                                                                    │
│  5. TokenStorageService.EndAllSessionsAsync("abc-123-def")        │
│     → UPDATE UserSessions SET IsActive = 0, EndedUtc = GETUTCDATE()│
│       WHERE UserId = 'abc-123-def'                                │
│                                                                    │
│  DATABASE AFTER LOGOUT:                                           │
│  ┌────┬─────────────┬─────────┬──────────┐                       │
│  │ Id │ UserId      │ Product │ IsActive │                       │
│  ├────┼─────────────┼─────────┼──────────┤                       │
│  │ 1  │ abc-123-def │ Portal  │ 0 ✗      │                       │
│  │ 2  │ abc-123-def │ Learn   │ 0 ✗      │                       │
│  │ 3  │ abc-123-def │ Safe    │ 0 ✗      │                       │
│  └────┴─────────────┴─────────┴──────────┘                       │
│  All tokens deactivated across all products!                      │
│                                                                    │
│  RESULT:                                                          │
│  ───────                                                          │
│  • Browser: No tokens in any product's localStorage               │
│  • Entra ID: SSO cookie deleted                                   │
│  • Database: All tokens marked inactive                           │
│  • If user visits ANY product → must log in again                 │
└───────────────────────────────────────────────────────────────────┘
```

The logout code:

```csharp
// AgOneSsoService.Logout() — client side
public void Logout()
{
    // MSAL handles browser-side cleanup + Entra ID session
    _navigation.NavigateToLogout("authentication/logout", "/");
}

// AgOneAuthController.Logout() — server side
[HttpPost("logout")]
public async Task<IActionResult> Logout([FromQuery] bool allProducts = true)
{
    var userId = User.FindFirst("oid")?.Value;

    if (allProducts)
    {
        await _tokenStorage.DeactivateAllUserTokensAsync(userId);
        await _tokenStorage.EndAllSessionsAsync(userId);
    }
    return Ok(new { message = "Logged out successfully" });
}
```

---

## FLOW 8: CLEANUP (Background, every 6 hours)

```csharp
// TokenCleanupBackgroundService — runs automatically
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        await Task.Delay(TimeSpan.FromHours(6), stoppingToken);

        // Remove tokens that have been inactive for 30+ days
        await tokenStorage.CleanupExpiredTokensAsync(TimeSpan.FromDays(30));
        // DELETE FROM UserTokens WHERE IsActive = 0 AND UpdatedUtc < (NOW - 30 days)

        // Remove sessions that ended 90+ days ago
        await tokenStorage.CleanupExpiredSessionsAsync(TimeSpan.FromDays(90));
        // DELETE FROM UserSessions WHERE IsActive = 0 AND EndedUtc < (NOW - 90 days)
    }
}
```

---

## SUMMARY: Which code runs at each step

```
┌─────────────────────┬───────────────────────────────────────────────────────┐
│ WHAT HAPPENS        │ WHICH CODE FILE HANDLES IT                           │
├─────────────────────┼───────────────────────────────────────────────────────┤
│                     │                                                       │
│ User visits app     │ App.razor → AuthorizeRouteView                       │
│                     │                                                       │
│ Not logged in?      │ AgOneAuthGuard.razor.cs → TrySilentLoginAsync()      │
│                     │                                                       │
│ Silent login works? │ AgOneAuthStateProvider.cs → returns authenticated     │
│                     │                                                       │
│ Silent login fails? │ AgOneSsoService.cs → Login() → redirect to Portal    │
│                     │                                                       │
│ Login redirect      │ Authentication.razor → MSAL.js → Entra ID           │
│                     │                                                       │
│ After login         │ MSAL.js → stores tokens in localStorage              │
│                     │                                                       │
│ API call            │ AgOneAuthorizationMessageHandler → attaches Bearer   │
│                     │                                                       │
│ API receives token  │ AuthenticationExtensions.cs → JWT validation          │
│                     │                                                       │
│ Token saved to DB   │ TokenCaptureMiddleware.cs → TokenStorageService      │
│                     │                                                       │
│ Token refresh       │ MSAL.js (browser) or TokenStorageService (server)    │
│ (browser)           │                                                       │
│                     │                                                       │
│ Token refresh       │ TokenStorageService.ExchangeRefreshTokenAsync()       │
│ (server/DB)         │ → POST to Entra ID token endpoint                    │
│                     │                                                       │
│ Cross-product nav   │ AgOneSsoService.NavigateToProduct()                   │
│                     │ → Target app's MSAL → iframe + SSO cookie            │
│                     │                                                       │
│ Logout              │ AgOneSsoService.Logout() → MSAL clears browser       │
│                     │ AgOneAuthController.Logout() → DB tokens deactivated │
│                     │                                                       │
│ Cleanup             │ TokenCleanupBackgroundService → delete old records    │
│                     │                                                       │
│ Get user info       │ IAgOneSsoService → GetUserDisplayNameAsync() etc.    │
│                     │ AgOneAuthController → GET /api/agone-auth/me         │
│                     │                                                       │
└─────────────────────┴───────────────────────────────────────────────────────┘
```

---

## QUICK SETUP REMINDER

To enable everything shown above, you need these lines in each product:

**Blazor WASM Client — Program.cs (1 line):**
```csharp
builder.Services.AddAgOneSso(builder.Configuration);
```

**API Backend — Program.cs (6 lines):**
```csharp
// Services
builder.Services.AddAgOneSsoApiAuthentication(builder.Configuration);
builder.Services.AddAgOneSsoApiAuthorization(builder.Configuration);
builder.Services.AddAgOneSsoCors(builder.Configuration);
builder.Services.AddAgOneTokenStorage(builder.Configuration);
builder.Services.AddAgOneTokenCleanup();

// Middleware (in this order)
app.UseCors(CorsExtensions.AgOneCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.UseAgOneSsoValidation();
app.UseTokenCapture();
```

**Database (1 SQL script or 1 EF migration):**
```bash
# Option A: Run the SQL script
sqlcmd -S server -d database -i Data/Migrations/001_CreateTokenTables.sql

# Option B: EF Core migration (if entities added to your existing DbContext)
dotnet ef migrations add AddAgOneTokenStorage
dotnet ef database update
```
