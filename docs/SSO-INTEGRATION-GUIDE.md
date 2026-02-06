# AG ONE Multi-Product SSO Integration Guide

## Azure Entra ID / External ID - Single Sign-On for Blazor WebAssembly

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [How SSO Works Across Products](#2-how-sso-works-across-products)
3. [Azure Entra ID Setup (Prerequisites)](#3-azure-entra-id-setup-prerequisites)
4. [Solution File Structure](#4-solution-file-structure)
5. [Step-by-Step Integration Guide](#5-step-by-step-integration-guide)
6. [Configuration Reference](#6-configuration-reference)
7. [Protecting Routes and APIs](#7-protecting-routes-and-apis)
8. [Cross-Product Navigation](#8-cross-product-navigation)
9. [Troubleshooting](#9-troubleshooting)
10. [Security Checklist](#10-security-checklist)

---

## 1. Architecture Overview

```
                    ┌─────────────────────────────────────┐
                    │       Azure Entra External ID        │
                    │   (Single Tenant, Single User Flow)  │
                    │        B2C_1_SignUpSignIn             │
                    │                                      │
                    │   SSO Session Cookie (login.microsoft│
                    │   online.com / *.ciamlogin.com)      │
                    └──────────┬───────────────────────────┘
                               │
                               │ MSAL.js (same tenant, same user flow)
                               │ SSO via shared Entra ID session cookie
                               │
        ┌──────────────────────┼──────────────────────────────┐
        │                      │                               │
   ┌────▼─────┐    ┌─────────▼────────┐    ┌────────────────▼────────────┐
   │ AG ONE   │    │   Product Apps    │    │   Product Apps              │
   │ Portal   │    │   (Learn, Safe)   │    │   (Work, Pulse)            │
   │          │    │                   │    │                             │
   │ Blazor   │    │ Blazor WASM       │    │ Blazor WASM                │
   │ WASM     │    │ + .NET API        │    │ + .NET API                 │
   │ + .NET   │    │                   │    │                             │
   │ API      │    │ portal.domain.com │    │ work.domain.com            │
   │          │    │ learn.domain.com  │    │ pulse.domain.com           │
   │ Primary  │    │ safe.domain.com   │    │                             │
   │ Login    │    │                   │    │                             │
   └──────────┘    └───────────────────┘    └─────────────────────────────┘
```

**Key Concept**: All products share the **same Azure Entra ID tenant** and the **same Sign-Up/Sign-In user flow**. When a user logs into AG ONE Portal, Azure Entra ID creates a **session cookie**. When the user navigates to any other product, MSAL.js detects this existing session and **silently acquires tokens** without showing a login page.

---

## 2. How SSO Works Across Products

### Flow: User Logs In via AG ONE Portal, Then Navigates to Learn

```
1. User visits https://portal.agone.com
2. Portal's MSAL detects no token → redirects to Entra ID login page
3. User enters credentials in Entra ID (your existing SignUp/SignIn flow)
4. Entra ID creates SSO session cookie + issues tokens
5. User is redirected back to Portal with tokens
6. User clicks "Learn" in the Portal navigation
7. Browser navigates to https://learn.agone.com
8. Learn's MSAL detects no local token → attempts SILENT token acquisition
9. MSAL uses iframe to check Entra ID session (SSO cookie exists!)
10. Entra ID recognizes the session → silently issues tokens for Learn
11. User is now authenticated in Learn WITHOUT seeing any login page!
```

### Flow: User Tries to Access Learn Directly via URL (Not Logged In)

```
1. User visits https://learn.agone.com directly
2. Learn's MSAL detects no token → attempts silent login
3. Silent login fails (no Entra ID session cookie)
4. Learn redirects to AG ONE Portal: https://portal.agone.com?returnUrl=https://learn.agone.com
5. Portal's MSAL redirects to Entra ID login page
6. User logs in
7. Portal redirects back to: https://learn.agone.com
8. Learn's MSAL now succeeds with silent login (session cookie exists)
9. User is authenticated in Learn
```

---

## 3. Azure Entra ID Setup (Prerequisites)

### 3.1 You Already Have

- Azure Entra External ID (or B2C) tenant
- A Sign-Up/Sign-In user flow (e.g., `B2C_1_SignUpSignIn`)
- At least one App Registration for AG ONE Portal

### 3.2 What You Need to Add/Verify

#### Option A: Single App Registration (Simpler)

Use ONE App Registration for all products. Add all redirect URIs:

1. Go to **Azure Portal** → **Entra ID** → **App Registrations** → Your App
2. Under **Authentication** → **Platform configurations** → **Single-page application**
3. Add ALL redirect URIs:

```
https://portal.yourdomain.com/authentication/login-callback
https://learn.yourdomain.com/authentication/login-callback
https://safe.yourdomain.com/authentication/login-callback
https://work.yourdomain.com/authentication/login-callback
https://pulse.yourdomain.com/authentication/login-callback
```

4. Under **Logout URL**, add:

```
https://portal.yourdomain.com/authentication/logout-callback
https://learn.yourdomain.com/authentication/logout-callback
https://safe.yourdomain.com/authentication/logout-callback
https://work.yourdomain.com/authentication/logout-callback
https://pulse.yourdomain.com/authentication/logout-callback
```

5. Ensure **Implicit grant and hybrid flows** has "ID tokens" checked (for Blazor WASM)
6. Ensure **Access tokens** is checked under implicit grant

#### Option B: Separate App Registrations (Better Isolation)

Create a separate App Registration for each product. All under the **same tenant**.

For each product App Registration:
1. Create App Registration (e.g., "AG ONE Learn")
2. Add redirect URI: `https://learn.yourdomain.com/authentication/login-callback`
3. Create an API App Registration for the backend
4. Expose an API scope: `api://learn-api-client-id/access_as_user`
5. Grant the frontend app permission to the API scope

**IMPORTANT**: All App Registrations must be in the **same Entra ID tenant** for SSO to work.

#### 3.3 API App Registration (For Each Product Backend)

For each product API:
1. Go to **App Registrations** → Create new (e.g., "AG ONE Learn API")
2. Under **Expose an API**:
   - Set Application ID URI: `api://YOUR-API-CLIENT-ID`
   - Add scope: `access_as_user`
3. Under **API permissions** of the FRONTEND app:
   - Add permission → My APIs → Select the API → Select `access_as_user`
   - Grant admin consent

#### 3.4 Token Configuration

In each App Registration, ensure these **optional claims** are configured:
1. Go to **Token configuration** → **Add optional claim**
2. Token type: **ID Token**
   - Add: `email`, `preferred_username`, `given_name`, `family_name`
3. Token type: **Access Token**
   - Add: `email`, `preferred_username`

---

## 4. Solution File Structure

```
YOUR EXISTING SOLUTION/
│
├── src/
│   ├── Shared/
│   │   ├── AgOne.Shared.Auth/                    ← ADD: Shared WASM auth library
│   │   │   ├── AgOne.Shared.Auth.csproj
│   │   │   ├── _Imports.razor
│   │   │   ├── Configuration/
│   │   │   │   └── AgOneSsoSettings.cs
│   │   │   ├── Extensions/
│   │   │   │   └── ServiceCollectionExtensions.cs
│   │   │   ├── Handlers/
│   │   │   │   └── AgOneAuthorizationMessageHandler.cs
│   │   │   ├── Providers/
│   │   │   │   └── AgOneAuthStateProvider.cs
│   │   │   ├── Components/
│   │   │   │   ├── AgOneAuthGuard.razor / .razor.cs
│   │   │   │   ├── AgOneRedirectToLogin.razor
│   │   │   │   ├── AgOneLoginDisplay.razor
│   │   │   │   ├── AgOneProductNavigator.razor
│   │   │   │   └── Authentication.razor
│   │   │   └── Services/
│   │   │       ├── IAgOneSsoService.cs
│   │   │       └── AgOneSsoService.cs
│   │   │
│   │   └── AgOne.Shared.Auth.Api/                ← ADD: Shared API auth library
│   │       ├── AgOne.Shared.Auth.Api.csproj
│   │       ├── Configuration/
│   │       │   └── AgOneApiAuthSettings.cs
│   │       ├── Extensions/
│   │       │   ├── AuthenticationExtensions.cs
│   │       │   ├── AuthorizationExtensions.cs
│   │       │   └── CorsExtensions.cs
│   │       ├── Middleware/
│   │       │   └── AgOneSsoValidationMiddleware.cs
│   │       └── Handlers/
│   │           └── ProductAccessRequirementHandler.cs
│   │
│   ├── AgOne.Portal/                              ← MODIFY: Your existing Portal
│   │   ├── Client/
│   │   │   ├── Program.cs                         ← ADD: builder.Services.AddAgOneSso(...)
│   │   │   ├── App.razor                          ← MODIFY: Add AuthorizeRouteView
│   │   │   ├── MainLayout.razor                   ← MODIFY: Add AgOneLoginDisplay
│   │   │   ├── _Imports.razor                     ← ADD: using statements
│   │   │   ├── Pages/
│   │   │   │   └── Authentication.razor           ← ADD: Copy from Components
│   │   │   └── wwwroot/
│   │   │       └── appsettings.json               ← ADD: AgOneSso section
│   │   └── Server/
│   │       ├── Program.cs                         ← ADD: 7 lines of code
│   │       └── appsettings.json                   ← ADD: AzureAd + AgOneSso sections
│   │
│   ├── AgOne.Learn/                               ← MODIFY: Your existing Learn product
│   │   ├── Client/ (same changes as Portal)
│   │   └── Server/ (same changes as Portal)
│   │
│   ├── AgOne.Safe/                                ← MODIFY: Your existing Safe product
│   │   ├── Client/ (same changes as Portal)
│   │   └── Server/ (same changes as Portal)
│   │
│   ├── AgOne.Work/                                ← MODIFY: Your existing Work product
│   │   ├── Client/ (same changes as Portal)
│   │   └── Server/ (same changes as Portal)
│   │
│   └── AgOne.Pulse/                               ← MODIFY: Your existing Pulse product
│       ├── Client/ (same changes as Portal)
│       └── Server/ (same changes as Portal)
```

---

## 5. Step-by-Step Integration Guide

### Step 1: Add the Shared Auth Projects to Your Solution

```bash
# From your solution root directory

# Add the shared projects to your solution
dotnet sln add src/Shared/AgOne.Shared.Auth/AgOne.Shared.Auth.csproj
dotnet sln add src/Shared/AgOne.Shared.Auth.Api/AgOne.Shared.Auth.Api.csproj
```

### Step 2: Add Project References to Each Product

#### For each Blazor WASM Client project:

```bash
# Portal Client
dotnet add src/AgOne.Portal/Client/AgOne.Portal.Client.csproj reference src/Shared/AgOne.Shared.Auth/AgOne.Shared.Auth.csproj

# Learn Client
dotnet add src/AgOne.Learn/Client/AgOne.Learn.Client.csproj reference src/Shared/AgOne.Shared.Auth/AgOne.Shared.Auth.csproj

# Safe Client
dotnet add src/AgOne.Safe/Client/AgOne.Safe.Client.csproj reference src/Shared/AgOne.Shared.Auth/AgOne.Shared.Auth.csproj

# Work Client
dotnet add src/AgOne.Work/Client/AgOne.Work.Client.csproj reference src/Shared/AgOne.Shared.Auth/AgOne.Shared.Auth.csproj

# Pulse Client
dotnet add src/AgOne.Pulse/Client/AgOne.Pulse.Client.csproj reference src/Shared/AgOne.Shared.Auth/AgOne.Shared.Auth.csproj
```

#### For each .NET API Server project:

```bash
# Portal Server
dotnet add src/AgOne.Portal/Server/AgOne.Portal.Server.csproj reference src/Shared/AgOne.Shared.Auth.Api/AgOne.Shared.Auth.Api.csproj

# Learn Server
dotnet add src/AgOne.Learn/Server/AgOne.Learn.Server.csproj reference src/Shared/AgOne.Shared.Auth.Api/AgOne.Shared.Auth.Api.csproj

# Safe Server
dotnet add src/AgOne.Safe/Server/AgOne.Safe.Server.csproj reference src/Shared/AgOne.Shared.Auth.Api/AgOne.Shared.Auth.Api.csproj

# Work Server
dotnet add src/AgOne.Work/Server/AgOne.Work.Server.csproj reference src/Shared/AgOne.Shared.Auth.Api/AgOne.Shared.Auth.Api.csproj

# Pulse Server
dotnet add src/AgOne.Pulse/Server/AgOne.Pulse.Server.csproj reference src/Shared/AgOne.Shared.Auth.Api/AgOne.Shared.Auth.Api.csproj
```

### Step 3: Modify Each Blazor WASM Client's Program.cs

Add this single line to each product's `Program.cs` (after `WebAssemblyHostBuilder.CreateDefault`):

```csharp
using AgOne.Shared.Auth.Extensions;

// ... your existing code ...

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// ADD THIS ONE LINE:
builder.Services.AddAgOneSso(builder.Configuration);

// ... rest of your existing code ...
```

### Step 4: Modify Each .NET API Server's Program.cs

Add these lines to each product's server `Program.cs`:

```csharp
using AgOne.Shared.Auth.Api.Extensions;
using AgOne.Shared.Auth.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ADD THESE 3 LINES (after builder creation, before builder.Build()):
builder.Services.AddAgOneSsoApiAuthentication(builder.Configuration);
builder.Services.AddAgOneSsoApiAuthorization(builder.Configuration);
builder.Services.AddAgOneSsoCors(builder.Configuration);

// ... your existing service registrations ...

var app = builder.Build();

// ADD THESE 4 LINES (in the middleware pipeline, before MapControllers):
app.UseCors(AgOne.Shared.Auth.Api.Extensions.CorsExtensions.AgOneCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.UseAgOneSsoValidation();

// ... rest of your existing middleware ...
```

### Step 5: Update App.razor in Each Product

Replace or modify your `App.razor` to include authentication:

```razor
@using AgOne.Shared.Auth.Components
@using Microsoft.AspNetCore.Components.Authorization

<CascadingAuthenticationState>
    <Router AppAssembly="@typeof(App).Assembly">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)">
                <NotAuthorized>
                    @if (context.User.Identity?.IsAuthenticated != true)
                    {
                        <AgOneRedirectToLogin />
                    }
                    else
                    {
                        <p>You are not authorized to access this resource.</p>
                    }
                </NotAuthorized>
                <Authorizing>
                    <p>Checking authorization...</p>
                </Authorizing>
            </AuthorizeRouteView>
            <FocusOnNavigate RouteData="@routeData" Selector="h1" />
        </Found>
        <NotFound>
            <PageTitle>Not found</PageTitle>
            <LayoutView Layout="@typeof(MainLayout)">
                <p>Sorry, there's nothing at this address.</p>
            </LayoutView>
        </NotFound>
    </Router>
</CascadingAuthenticationState>
```

### Step 6: Add Authentication.razor Page to Each Product

Copy `Authentication.razor` to each product's `Pages` folder:

**File: `YourProduct.Client/Pages/Authentication.razor`**

```razor
@page "/authentication/{action}"
@using Microsoft.AspNetCore.Components.WebAssembly.Authentication

<RemoteAuthenticatorView Action="@Action">
    <LogInFailed>
        <h3>Login Failed</h3>
        <p>There was an error signing you in. Please try again.</p>
        <a href="/">Return to Home</a>
    </LogInFailed>
    <LogOutSucceeded>
        <h3>Signed Out</h3>
        <p>You have been signed out successfully.</p>
        <a href="/">Return to Home</a>
    </LogOutSucceeded>
</RemoteAuthenticatorView>

@code {
    [Parameter] public string? Action { get; set; }
}
```

### Step 7: Add _Imports.razor Using Statements

Add these to your existing `_Imports.razor` in each Client project:

```razor
@using Microsoft.AspNetCore.Authorization
@using Microsoft.AspNetCore.Components.Authorization
@using Microsoft.AspNetCore.Components.WebAssembly.Authentication
@using AgOne.Shared.Auth.Components
@using AgOne.Shared.Auth.Services
```

### Step 8: Configure appsettings.json

#### For each Blazor WASM Client (`wwwroot/appsettings.json`):

Copy the appropriate `AgOneSso` section from the `config/` folder:
- Portal: `config/appsettings.Portal.Client.json`
- Learn: `config/appsettings.Learn.Client.json`
- Safe: `config/appsettings.Safe.Client.json`
- Work: `config/appsettings.Work.Client.json`
- Pulse: `config/appsettings.Pulse.Client.json`

**Replace ALL placeholder values** (`YOUR-TENANT`, `YOUR-CLIENT-ID`, etc.) with your actual Azure values.

#### For each .NET API Server (`appsettings.json`):

Merge the `AzureAd` and `AgOneSso` sections from:
- Portal: `config/appsettings.Portal.Server.json`
- Learn: `config/appsettings.Learn.Server.json`
- Safe: `config/appsettings.Safe.Server.json`
- Work: `config/appsettings.Work.Server.json`
- Pulse: `config/appsettings.Pulse.Server.json`

### Step 9: Add Login Display to MainLayout (Optional but Recommended)

In your `MainLayout.razor`, add the login display component:

```razor
@using AgOne.Shared.Auth.Components

<div class="top-row px-4">
    <AgOneProductNavigator />
    <AgOneLoginDisplay />
</div>
```

---

## 6. Configuration Reference

### Client-Side (Blazor WASM) - `wwwroot/appsettings.json`

| Setting | Portal Value | Product Value | Description |
|---------|-------------|---------------|-------------|
| `Authority` | Same for all | Same for all | Entra ID authority URL |
| `ClientId` | Portal's client ID | Product's client ID | Azure App Registration ID |
| `SignUpSignInPolicyId` | Your policy name | Same policy name | User flow name |
| `DefaultScopes` | Portal API scopes | Product API scopes | API scopes to request |
| `AgOnePortalUrl` | Portal URL | Portal URL | Where to redirect for login |
| `ProductName` | `"Portal"` | `"Learn"` / `"Safe"` etc. | Current product identifier |
| `RedirectToPortalOnUnauthenticated` | `false` | `true` | **KEY DIFFERENCE** |
| `ApiBaseUrl` | Portal API URL | Product API URL | Backend API base URL |

### Server-Side (.NET API) - `appsettings.json`

| Setting | Description |
|---------|-------------|
| `AzureAd:Instance` | Entra ID instance URL |
| `AzureAd:TenantId` | Your tenant ID |
| `AzureAd:ClientId` | API's App Registration client ID |
| `AzureAd:Audience` | API's Application ID URI |
| `AgOneSso:AllowedOrigins` | ALL product frontend URLs (for CORS) |
| `AgOneSso:ValidIssuers` | Valid token issuer URLs |

---

## 7. Protecting Routes and APIs

### Protecting Blazor Pages

Use the `[Authorize]` attribute on any page that requires authentication:

```razor
@page "/my-protected-page"
@attribute [Authorize]

<h1>This page requires authentication</h1>

@* User info is available via the cascading AuthenticationState *@
<AuthorizeView>
    <Authorized>
        <p>Welcome, @context.User.Identity?.Name!</p>
    </Authorized>
</AuthorizeView>
```

### Protecting API Endpoints

Use `[Authorize]` with optional policies on controllers:

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AgOne.Authenticated")]  // Requires AG ONE SSO login
public class MyController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var userId = User.FindFirst("oid")?.Value;
        var email = User.FindFirst("preferred_username")?.Value;
        return Ok(new { userId, email });
    }

    [HttpGet("admin")]
    [Authorize(Policy = "AgOne.Admin")]  // Requires Admin role
    public IActionResult AdminOnly()
    {
        return Ok("Admin access granted");
    }
}
```

### Available Authorization Policies

| Policy | Description |
|--------|-------------|
| `AgOne.Authenticated` | User must be authenticated |
| `AgOne.ValidUser` | User must have a valid OID claim |
| `AgOne.Portal.Access` | Product-specific access (customizable) |
| `AgOne.Learn.Access` | Product-specific access (customizable) |
| `AgOne.Safe.Access` | Product-specific access (customizable) |
| `AgOne.Work.Access` | Product-specific access (customizable) |
| `AgOne.Pulse.Access` | Product-specific access (customizable) |
| `AgOne.Admin` | User must have Admin or GlobalAdmin role |
| `AgOne.Manager` | User must have Admin, GlobalAdmin, or Manager role |

---

## 8. Cross-Product Navigation

### Using the SSO Service in Your Components

```razor
@inject IAgOneSsoService SsoService

<button @onclick="GoToLearn">Open Learn</button>
<button @onclick="GoToSafe">Open Safe</button>

<p>Logged in as: @_userName</p>

@code {
    private string? _userName;

    protected override async Task OnInitializedAsync()
    {
        _userName = await SsoService.GetUserDisplayNameAsync();
    }

    private void GoToLearn()
    {
        // User will be silently authenticated in Learn via SSO
        SsoService.NavigateToProduct("Learn");
    }

    private void GoToSafe()
    {
        SsoService.NavigateToProduct("Safe", "/dashboard");
    }
}
```

### Using the Product Navigator Component

Simply add to your layout:

```razor
<AgOneProductNavigator />
```

This shows navigation links to all configured products.

---

## 9. Troubleshooting

### Common Issues

#### 1. "AADSTS50011: The redirect URI does not match"

**Fix**: Add the exact redirect URI to your Azure App Registration:
- `https://your-product.domain.com/authentication/login-callback`
- Make sure it's added under **Single-page application** platform (NOT Web).

#### 2. "Silent login fails - user sees login page on every product"

**Possible causes**:
- Third-party cookies are blocked in the browser (Chrome blocks them by default in Incognito)
- Products are using different Entra ID tenants (they must use the SAME tenant)
- Different user flow policies between products

**Fix**:
- Ensure all products use the same `Authority` and `SignUpSignInPolicyId`
- Test in a browser with third-party cookies enabled
- Consider using `loginRedirect` instead of `loginPopup` (already configured)

#### 3. "CORS error when calling API from another product"

**Fix**: Make sure ALL product frontend URLs are in the `AllowedOrigins` array in each API's `appsettings.json`.

#### 4. "Token audience mismatch"

**Fix**: Ensure the `DefaultScopes` in the client config match the `Audience` in the server config.
- Client: `"api://YOUR-API-CLIENT-ID/access_as_user"`
- Server: `"Audience": "api://YOUR-API-CLIENT-ID"`

#### 5. "User is authenticated but claims are empty"

**Fix**: Configure optional claims in Azure:
1. App Registration → Token configuration → Add optional claim
2. Add `email`, `preferred_username`, `name` to both ID and Access tokens

### Debug Logging

Enable MSAL logging in development by setting in `appsettings.Development.json`:

```json
{
  "AgOneSso": {
    "EnableMsalLogging": true
  }
}
```

---

## 10. Security Checklist

Before going to production, verify:

- [ ] All `appsettings.json` placeholder values replaced with real Azure values
- [ ] `RedirectToPortalOnUnauthenticated` is `false` ONLY for Portal, `true` for all products
- [ ] All redirect URIs registered in Azure App Registration(s)
- [ ] CORS `AllowedOrigins` contains ALL product URLs (not wildcards)
- [ ] API `Audience` matches the scope prefix in client `DefaultScopes`
- [ ] Token issuer validation is enabled (`ValidateIssuer: true`)
- [ ] `ValidIssuers` list contains the correct issuer URL(s) for your tenant
- [ ] HTTPS is enforced on all product URLs
- [ ] `EnableMsalLogging` is `false` in production
- [ ] All API controllers have `[Authorize]` attributes
- [ ] Sensitive endpoints use appropriate authorization policies
- [ ] Third-party cookie warnings are handled (show message to users if needed)

---

## Quick Summary: What to Add to Each Product

### Per Blazor WASM Client (5 changes):

1. **Project Reference** → `AgOne.Shared.Auth`
2. **Program.cs** → Add `builder.Services.AddAgOneSso(builder.Configuration);`
3. **App.razor** → Wrap with `CascadingAuthenticationState` + `AuthorizeRouteView`
4. **Pages/Authentication.razor** → Copy the authentication callback page
5. **wwwroot/appsettings.json** → Add `AgOneSso` configuration section

### Per .NET API Server (3 changes):

1. **Project Reference** → `AgOne.Shared.Auth.Api`
2. **Program.cs** → Add 3 service lines + 4 middleware lines
3. **appsettings.json** → Add `AzureAd` + `AgOneSso` configuration sections

### Total: 8 changes per product, all copy-paste with config adjustments.

---

## Appendix: Entra ID Type-Specific Authority URLs

| Entra ID Type | Authority Format | Example |
|---------------|-----------------|---------|
| Entra ID (Work/School) | `https://login.microsoftonline.com/{tenant-id}` | `https://login.microsoftonline.com/abc123-def456/` |
| Entra External ID (CIAM) | `https://{tenant-name}.ciamlogin.com/` | `https://mycompany.ciamlogin.com/` |
| Azure AD B2C | `https://{tenant}.b2clogin.com/{tenant}.onmicrosoft.com/{policy}` | `https://mycompany.b2clogin.com/mycompany.onmicrosoft.com/B2C_1_SignUpSignIn` |
