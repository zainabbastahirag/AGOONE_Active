# AG ONE - Refresh Token Flow & Database Token Storage

## Table of Contents

1. [How Tokens Work in Your Setup](#1-how-tokens-work-in-your-setup)
2. [The Three Token Types](#2-the-three-token-types)
3. [Refresh Token Flow - Step by Step](#3-refresh-token-flow---step-by-step)
4. [Where Tokens Live (Browser vs Database)](#4-where-tokens-live-browser-vs-database)
5. [Saving Tokens to the Database](#5-saving-tokens-to-the-database)
6. [Database Schema](#6-database-schema)
7. [Integration Steps](#7-integration-steps)
8. [Using Stored Tokens in Your Code](#8-using-stored-tokens-in-your-code)
9. [Important Notes About SPA Refresh Tokens](#9-important-notes-about-spa-refresh-tokens)
10. [Token Lifecycle Diagrams](#10-token-lifecycle-diagrams)

---

## 1. How Tokens Work in Your Setup

Your setup has two "layers" of token management:

```
┌─────────────────────────────────────────────────────────────┐
│  LAYER 1: BROWSER (Blazor WASM + MSAL.js)                  │
│                                                              │
│  MSAL.js handles ALL token operations automatically:         │
│  - Gets tokens from Entra ID after login                     │
│  - Stores them in localStorage/sessionStorage                │
│  - Silently refreshes expired tokens using SSO cookies       │
│  - Attaches tokens to HTTP requests via AuthorizationHandler │
│                                                              │
│  You DON'T need to manually manage tokens on the client.     │
│  MSAL.js does it for you.                                    │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           │  Bearer token sent on every API call
                           │
┌──────────────────────────▼──────────────────────────────────┐
│  LAYER 2: API BACKEND (.NET API + Database)                  │
│                                                              │
│  The API receives the Bearer token on every request.         │
│  TokenCaptureMiddleware extracts it and stores it in the DB. │
│                                                              │
│  Why store in DB?                                            │
│  - Background jobs that call APIs on behalf of users         │
│  - Server-to-server calls using user tokens                  │
│  - Audit trail of all authentication events                  │
│  - Centralized token revocation                              │
│  - Session tracking across products                          │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. The Three Token Types

### Access Token (JWT)
```
Lifetime:  1 hour (default, configurable in Entra ID)
Purpose:   Authorizes API calls. Sent as "Bearer {token}" header.
Content:   User ID, email, roles, scopes, expiry time
Size:      ~1-2 KB
Stored:    Browser (MSAL) + Database (via TokenCapture middleware)
```

### Refresh Token
```
Lifetime:  24 hours to 90 days (configurable in Entra ID)
Purpose:   Gets NEW access tokens without user interaction
Content:   Opaque string (not a JWT, you can't read it)
Size:      ~500 bytes
Stored:    Browser (MSAL manages it internally) + Database (if available)
```

### ID Token (JWT)
```
Lifetime:  1 hour
Purpose:   Contains user profile information (name, email, etc.)
Content:   User claims for the UI
Size:      ~1-2 KB
Stored:    Browser (MSAL) + Database (optionally)
```

---

## 3. Refresh Token Flow - Step by Step

### Flow A: Browser-Side (Automatic - MSAL handles this)

This is what happens automatically in the Blazor WASM client:

```
Step 1: User logs in via Entra ID
        ─────────────────────────────────────
        User → Entra ID login page → Enters credentials
        Entra ID → Returns: access_token + id_token + refresh_token
        MSAL.js → Stores all tokens in localStorage

Step 2: User uses the app (0-55 minutes)
        ─────────────────────────────────────
        App → Makes API call
        AuthorizationHandler → Checks: Is access_token still valid?
        YES → Attaches "Bearer {access_token}" to request
        API → Validates JWT → Returns data

Step 3: Access token is about to expire (~55 minutes)
        ─────────────────────────────────────
        App → Makes API call
        AuthorizationHandler → Checks: Is access_token still valid?
        NO (expired or <5 min left)

        MSAL.js AUTOMATICALLY tries these methods (in order):

        Method 1: Silent token refresh via hidden iframe
        ─────────────────────────────────────────────────
        MSAL.js → Creates hidden iframe to Entra ID
        iframe → POST to https://{authority}/oauth2/v2.0/authorize
                 with prompt=none (no UI shown)
        Entra ID → Checks SSO session cookie
        Cookie valid? → Returns new access_token via iframe
        MSAL.js → Updates localStorage with new token
        ✅ User sees nothing, app continues working

        Method 2: Refresh token grant (if Method 1 fails)
        ─────────────────────────────────────────────────
        MSAL.js → POST to https://{authority}/oauth2/v2.0/token
        Body: grant_type=refresh_token
              &client_id={clientId}
              &refresh_token={stored_refresh_token}
              &scope=openid profile email api://xxx/access_as_user
        Entra ID → Validates refresh_token
        Valid? → Returns new access_token + new refresh_token
        MSAL.js → Updates localStorage
        ✅ User sees nothing, app continues working

        Method 3: If both fail (session expired, user inactive too long)
        ─────────────────────────────────────────────────────────────
        MSAL.js → Cannot get a token silently
        AuthorizationHandler → Throws AccessTokenNotAvailableException
        AgOneRedirectToLogin → Redirects user to Entra ID login page
        User must enter credentials again
```

### Flow B: Server-Side (Manual - Using stored refresh tokens from DB)

This is for when your **API backend** needs to refresh tokens (e.g., background jobs):

```
Step 1: Your background job needs to call an API on behalf of a user
        ─────────────────────────────────────
        BackgroundJob → Calls ITokenStorageService.GetValidAccessTokenAsync(userId, "Learn")

Step 2: Service checks the database
        ─────────────────────────────────────
        TokenStorageService → SELECT * FROM UserTokens
                              WHERE UserId = @userId AND ProductName = 'Learn' AND IsActive = 1
        
        Found token? → Check: AccessTokenExpiresUtc > GETUTCDATE() + 5 minutes?
        YES → Return the stored access token ✅

Step 3: Access token expired → Use refresh token
        ─────────────────────────────────────
        TokenStorageService → Takes the stored RefreshToken from DB
        → POST to https://{authority}/oauth2/v2.0/token
           Body: grant_type=refresh_token
                 &client_id={clientId}
                 &refresh_token={stored_refresh_token}
                 &scope={scopes}
        
        Entra ID → Validates refresh token
        Valid? → Returns: new access_token + (possibly) new refresh_token
        
        TokenStorageService → UPDATE UserTokens
                              SET AccessToken = @newAccessToken,
                                  RefreshToken = @newRefreshToken,
                                  AccessTokenExpiresUtc = @newExpiry,
                                  RefreshCount = RefreshCount + 1,
                                  UpdatedUtc = GETUTCDATE()
                              WHERE UserId = @userId AND ProductName = 'Learn'
        
        Return new access_token ✅

Step 4: Refresh token also expired
        ─────────────────────────────────────
        Entra ID → Returns 400 Bad Request (invalid_grant)
        TokenStorageService → UPDATE UserTokens SET IsActive = 0
        → Returns null (user must re-authenticate via browser)
```

---

## 4. Where Tokens Live (Browser vs Database)

```
┌───────────────────────────────────────────────────────────────┐
│                    TOKEN LIFECYCLE                              │
├───────────────────────────────────────────────────────────────┤
│                                                                │
│  1. LOGIN                                                      │
│     Entra ID issues tokens → MSAL stores in browser localStorage│
│                                                                │
│  2. FIRST API CALL                                             │
│     Browser sends Bearer token → API receives it               │
│     TokenCaptureMiddleware → Extracts token → Saves to DB      │
│                                                                │
│  3. SUBSEQUENT API CALLS (while token valid)                   │
│     Browser sends same token → API validates JWT               │
│     TokenCaptureMiddleware → Updates DB (UpdatedUtc, etc.)     │
│                                                                │
│  4. TOKEN EXPIRES (browser side)                               │
│     MSAL.js silently refreshes → Gets new token                │
│     Next API call sends new token → TokenCapture updates DB    │
│                                                                │
│  5. TOKEN EXPIRES (server side, background job)                │
│     Job calls GetValidAccessTokenAsync()                       │
│     Service finds expired token in DB                          │
│     Service uses stored refresh token to get new one           │
│     Service updates DB with new tokens                         │
│                                                                │
│  6. LOGOUT                                                     │
│     Browser → MSAL clears localStorage                         │
│     Client → Calls /api/agone-auth/logout                      │
│     API → Sets IsActive=false on all user tokens in DB         │
│                                                                │
│  7. CLEANUP (background service, every 6 hours)                │
│     Removes inactive tokens older than 30 days                 │
│     Removes ended sessions older than 90 days                  │
│                                                                │
└───────────────────────────────────────────────────────────────┘
```

---

## 5. Saving Tokens to the Database

### How Tokens Get Into the Database

There are **two paths** for tokens to reach your database:

#### Path 1: Automatic Capture (Recommended)

The `TokenCaptureMiddleware` intercepts every authenticated API request, extracts the Bearer token, and saves/updates it in the database.

```
Browser → API Call with Bearer token → TokenCaptureMiddleware → Database
```

You don't write any code for this. Just register the middleware:
```csharp
app.UseTokenCapture(); // In Program.cs, after UseAuthentication + UseAuthorization
```

#### Path 2: Explicit Sync from Client (Optional)

The `TokenSyncService` in the Blazor client can explicitly send tokens to a dedicated API endpoint. This is useful when you need to store the refresh token (which is NOT sent in normal API calls).

```csharp
// In your Blazor component, after login
@inject ITokenSyncService TokenSync

protected override async Task OnInitializedAsync()
{
    await TokenSync.SyncTokenToBackendAsync();
}
```

---

## 6. Database Schema

### UserTokens Table

| Column | Type | Description |
|--------|------|-------------|
| Id | BIGINT (PK) | Auto-increment ID |
| UserId | NVARCHAR(128) | Entra ID Object ID (oid) |
| Email | NVARCHAR(256) | User's email |
| DisplayName | NVARCHAR(256) | User's display name |
| ProductName | NVARCHAR(50) | Portal, Learn, Safe, Work, Pulse |
| AccessToken | NVARCHAR(MAX) | The JWT access token |
| RefreshToken | NVARCHAR(MAX) | The refresh token (if available) |
| IdToken | NVARCHAR(MAX) | The ID token (optional) |
| AccessTokenExpiresUtc | DATETIME2 | When the access token expires |
| RefreshTokenExpiresUtc | DATETIME2 | When the refresh token expires |
| GrantedScopes | NVARCHAR(1024) | Space-separated scopes |
| TenantId | NVARCHAR(128) | Entra ID tenant ID |
| IsActive | BIT | Active/revoked flag |
| CreatedUtc | DATETIME2 | First creation timestamp |
| UpdatedUtc | DATETIME2 | Last update timestamp |
| LastIpAddress | NVARCHAR(45) | Client IP address |
| LastUserAgent | NVARCHAR(512) | Client user agent |
| RefreshCount | INT | Number of refreshes |

**Unique Index**: (UserId, ProductName) - One token record per user per product.

### UserSessions Table

| Column | Type | Description |
|--------|------|-------------|
| Id | BIGINT (PK) | Auto-increment ID |
| SessionId | NVARCHAR(128) | Unique session GUID |
| UserId | NVARCHAR(128) | Entra ID Object ID |
| Email | NVARCHAR(256) | User's email |
| InitiatedByProduct | NVARCHAR(50) | Which product started the session |
| ProductsAccessed | NVARCHAR(512) | Comma-separated list of products visited |
| StartedUtc | DATETIME2 | Session start time |
| LastActivityUtc | DATETIME2 | Last API call time |
| EndedUtc | DATETIME2 | When session ended (logout) |
| IsActive | BIT | Active/ended flag |
| IpAddress | NVARCHAR(45) | Client IP |
| UserAgent | NVARCHAR(512) | Client user agent |

---

## 7. Integration Steps

### Step 1: Create the Database Tables

**Option A: Run the SQL script directly**
```bash
sqlcmd -S your-server -d your-database -i src/Shared/AgOne.Shared.Auth.Api/Data/Migrations/001_CreateTokenTables.sql
```

**Option B: Add to your existing DbContext and use EF migrations**

In your existing DbContext:
```csharp
public class YourAppDbContext : DbContext
{
    // Your existing DbSets...
    public DbSet<Product> Products { get; set; }
    
    // ADD THESE TWO LINES:
    public DbSet<UserToken> UserTokens { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Your existing configurations...

        // ADD THIS LINE:
        AgOneTokenDbContext.ConfigureAgOneTokenEntities(modelBuilder);
    }
}
```

Then run:
```bash
dotnet ef migrations add AddAgOneTokenStorage
dotnet ef database update
```

### Step 2: Add to API Program.cs (3 lines)

```csharp
// After your existing AddAgOneSso lines, add:
builder.Services.AddAgOneTokenStorage(builder.Configuration);  // Token storage
builder.Services.AddAgOneTokenCleanup();                       // Cleanup job

// In middleware pipeline, after UseAuthorization:
app.UseTokenCapture();  // Auto-capture tokens from API requests
```

### Step 3: Add Connection String

In your API's `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "AgOneTokenDb": "Server=your-server;Database=your-database;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Or, if using your existing database, no additional connection string needed (Option B).

### Step 4: Register the Auth Controller

In Program.cs, make sure the shared controller is discovered:
```csharp
builder.Services.AddControllers()
    .AddApplicationPart(typeof(AgOne.Shared.Auth.Api.Controllers.AgOneAuthController).Assembly);
```

---

## 8. Using Stored Tokens in Your Code

### 8.1: Getting a Valid Token for Background Jobs

```csharp
public class MyBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var tokenStorage = scope.ServiceProvider.GetRequiredService<ITokenStorageService>();

        // This will automatically refresh if expired
        var accessToken = await tokenStorage.GetValidAccessTokenAsync(
            userId: "user-oid-from-entra-id",
            productName: "Learn");

        if (accessToken != null)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
            
            var response = await httpClient.GetAsync("https://learn-api.agone.com/api/courses");
            // ... use the response ...
        }
        else
        {
            // No valid token available - user needs to re-authenticate
            // Log a warning or queue a notification
        }
    }
}
```

### 8.2: Checking User Session Status

```csharp
[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AgOne.Admin")]
public class AdminController : ControllerBase
{
    private readonly ITokenStorageService _tokenStorage;

    [HttpGet("user-sessions/{userId}")]
    public async Task<IActionResult> GetUserSessions(string userId)
    {
        var sessions = await _tokenStorage.GetActiveSessionsAsync(userId);
        var tokens = await _tokenStorage.GetAllUserTokensAsync(userId);

        return Ok(new
        {
            activeSessions = sessions,
            activeProducts = tokens.Select(t => t.ProductName),
            lastActivity = sessions.MaxBy(s => s.LastActivityUtc)?.LastActivityUtc
        });
    }

    [HttpPost("force-logout/{userId}")]
    public async Task<IActionResult> ForceLogout(string userId)
    {
        await _tokenStorage.DeactivateAllUserTokensAsync(userId);
        await _tokenStorage.EndAllSessionsAsync(userId);
        return Ok(new { message = $"Force-logged out user {userId} from all products" });
    }
}
```

### 8.3: Manually Triggering a Token Refresh

```csharp
// In your service/controller
var refreshResult = await _tokenStorage.RefreshTokenAsync(userId, "Learn");

if (refreshResult.Success)
{
    Console.WriteLine($"New token expires at: {refreshResult.ExpiresUtc}");
}
else
{
    Console.WriteLine($"Refresh failed: {refreshResult.ErrorMessage}");
}
```

---

## 9. Important Notes About SPA Refresh Tokens

### The Reality of Blazor WASM + Entra ID

**Important**: In a **Single Page Application (SPA)** flow like Blazor WebAssembly, Azure Entra ID handles token renewal primarily through **SSO session cookies**, NOT traditional refresh tokens.

Here's what you need to know:

```
┌─────────────────────────────────────────────────────────────────┐
│  SPA TOKEN RENEWAL (Your Blazor WASM apps)                      │
│                                                                  │
│  Primary Method: Silent iframe + SSO session cookie              │
│  ──────────────────────────────────────────────────              │
│  MSAL.js creates a hidden iframe pointing to Entra ID            │
│  The iframe carries the SSO session cookie                       │
│  Entra ID sees the cookie → issues new tokens via the iframe     │
│  NO refresh token needed for this flow                           │
│                                                                  │
│  Fallback Method: Refresh token (when available)                 │
│  ──────────────────────────────────────────────────              │
│  Some Entra ID configurations issue refresh tokens to SPAs       │
│  MSAL.js stores them in localStorage                             │
│  Used when iframe method fails (e.g., cross-origin issues)       │
│                                                                  │
│  When NEITHER works:                                             │
│  ──────────────────────────────────────────────────              │
│  User is redirected to login page (full redirect)                │
│  This happens when the SSO session has truly expired             │
│  (default: 24 hours of inactivity)                               │
└─────────────────────────────────────────────────────────────────┘
```

### When Will You Have Refresh Tokens in the Database?

| Scenario | Refresh Token Available? |
|----------|------------------------|
| User logs in via browser, MSAL handles everything | Maybe - depends on Entra ID config |
| TokenCaptureMiddleware captures from API request | **No** - Only the access token is sent in API headers |
| Client explicitly syncs via TokenSyncService | Only if MSAL exposes it to JavaScript (limited) |
| Confidential client (server app with client secret) | **Yes** - Always available |

### Recommendation

For **server-side token refresh** (background jobs, etc.), consider:

1. **Option A**: Use **MSAL.NET Confidential Client** in your API backend
   - Register a "Web" app in Entra ID (not SPA)
   - Configure a client secret or certificate
   - Use the On-Behalf-Of (OBO) flow to exchange the user's access token for tokens with broader scopes
   - This gives you a proper refresh token server-side

2. **Option B**: Rely on the client-side MSAL to keep tokens fresh
   - Every time the Blazor client makes an API call, it sends a fresh token
   - TokenCaptureMiddleware keeps the DB updated
   - For background jobs, check if the DB token is recent enough

3. **Option C** (Current Implementation): Store whatever tokens are available
   - If a refresh token is available, use it
   - If not, the stored access token works for up to 1 hour
   - After that, the user must make another API call from the browser to refresh

---

## 10. Token Lifecycle Diagrams

### Complete Token Lifecycle

```
Time ──────────────────────────────────────────────────────────►

T=0        LOGIN
           ┌──────────────────────┐
           │ User enters password │
           │ at Entra ID          │
           └───────┬──────────────┘
                   │
                   ▼
           Entra ID issues:
           • access_token  (expires T+60min)
           • id_token      (expires T+60min)
           • refresh_token (expires T+24h to T+90d)
                   │
                   ├──► MSAL stores in browser localStorage
                   │
                   ▼
T=0-55min  NORMAL USAGE
           Browser sends access_token with every API call
           API validates JWT → processes request
           TokenCaptureMiddleware → saves/updates token in DB
                   │
                   ▼
T=55min    TOKEN ABOUT TO EXPIRE
           MSAL.js detects token expiring soon
                   │
                   ├──► Method 1: iframe to Entra ID (prompt=none)
                   │    Success? → New tokens, continue
                   │
                   ├──► Method 2: Refresh token grant
                   │    POST /oauth2/v2.0/token (grant_type=refresh_token)
                   │    Success? → New tokens, continue
                   │
                   └──► Method 3: Both failed
                        → Redirect user to login
                   │
                   ▼
T=60min    NEW ACCESS TOKEN (if refresh succeeded)
           Browser has fresh access_token (expires T+120min)
           Next API call → TokenCapture updates DB with new token
                   │
                   ▼
T=24h+     REFRESH TOKEN EXPIRES (if user inactive)
           No more silent renewal possible
           User must log in again
                   │
                   ▼
           LOGOUT (or session timeout)
           MSAL.js → Clears browser storage
           Client → POST /api/agone-auth/logout
           API → Sets IsActive=false in DB
           API → Ends session record
```

### Cross-Product Token Flow

```
T=0        User logs into PORTAL
           Entra ID → issues portal tokens
           MSAL → stores in portal.agone.com localStorage
           API → TokenCapture saves Portal token to DB

T=5min     User clicks "Learn" in Portal navbar
           Browser navigates to learn.agone.com
           
T=5min     Learn's MSAL.js initializes
           No tokens in learn.agone.com localStorage
           MSAL → iframe to Entra ID (prompt=none)
           Entra ID → SSO cookie exists from Portal login!
           Entra ID → issues Learn tokens silently
           MSAL → stores in learn.agone.com localStorage
           
T=5min+    User uses Learn app
           API calls → TokenCapture saves Learn token to DB
           
           Database now has:
           ┌──────────────────────────────────────┐
           │ UserId  │ ProductName │ IsActive      │
           │ user123 │ Portal      │ true          │
           │ user123 │ Learn       │ true          │
           └──────────────────────────────────────┘

T=20min    User navigates to Safe
           Same flow: silent iframe → tokens → DB
           
           Database:
           ┌──────────────────────────────────────┐
           │ UserId  │ ProductName │ IsActive      │
           │ user123 │ Portal      │ true          │
           │ user123 │ Learn       │ true          │
           │ user123 │ Safe        │ true          │
           └──────────────────────────────────────┘

T=2hours   User clicks LOGOUT in any product
           Client → POST /api/agone-auth/logout?allProducts=true
           
           Database:
           ┌──────────────────────────────────────┐
           │ UserId  │ ProductName │ IsActive      │
           │ user123 │ Portal      │ false         │
           │ user123 │ Learn       │ false         │
           │ user123 │ Safe        │ false         │
           └──────────────────────────────────────┘
```

---

## Quick Reference: New Files Added

### API Backend (AgOne.Shared.Auth.Api):

| File | Purpose |
|------|---------|
| `Data/Entities/UserToken.cs` | Token entity (DB table) |
| `Data/Entities/UserSession.cs` | Session entity (DB table) |
| `Data/AgOneTokenDbContext.cs` | EF Core DbContext for token storage |
| `Data/Migrations/001_CreateTokenTables.sql` | SQL script to create tables |
| `Services/ITokenStorageService.cs` | Token storage interface + DTOs |
| `Services/TokenStorageService.cs` | Full implementation with refresh flow |
| `Services/TokenCleanupBackgroundService.cs` | Background cleanup job |
| `Middleware/TokenCaptureMiddleware.cs` | Auto-captures tokens from API requests |
| `Controllers/AgOneAuthController.cs` | Auth management API endpoints |
| `Extensions/TokenStorageExtensions.cs` | DI registration for token storage |

### Blazor WASM Client (AgOne.Shared.Auth):

| File | Purpose |
|------|---------|
| `Services/TokenSyncService.cs` | Syncs tokens from browser to API backend |

### API Program.cs Changes (3 new lines):

```csharp
builder.Services.AddAgOneTokenStorage(builder.Configuration);  // Line 1
builder.Services.AddAgOneTokenCleanup();                       // Line 2
// ...
app.UseTokenCapture();                                         // Line 3
```
