# AG ONE SSO - Step by Step Integration

Your existing solution structure (for each product):

```
YourProduct/
├── UI/              ← Blazor WebAssembly frontend
├── API/             ← .NET Web API backend
├── Infrastructure/
├── Domain/
└── Shared/
```

Products: **Portal**, **Learn**, **Safe**, **Work**, **Pulse**

Below is the exact order. Do steps 1-4 first. Then repeat steps 5-6 for each product.

---

## STEP 1: Add the two shared auth projects to your solution

Copy the entire `Step1_SharedAuthLibraries/` folder contents into your solution's Shared folder.

**What to copy:**

```
Step1_SharedAuthLibraries/
├── AgOne.Shared.Auth/           → Copy to:  YourSolution/Shared/AgOne.Shared.Auth/
└── AgOne.Shared.Auth.Api/       → Copy to:  YourSolution/Shared/AgOne.Shared.Auth.Api/
```

**Then add them to your .sln file:**

```bash
dotnet sln add Shared/AgOne.Shared.Auth/AgOne.Shared.Auth.csproj
dotnet sln add Shared/AgOne.Shared.Auth.Api/AgOne.Shared.Auth.Api.csproj
```

---

## STEP 2: Create the database tables

Run the SQL script against your database:

```bash
sqlcmd -S YOUR-SERVER -d YOUR-DATABASE -i Step4_DatabaseSetup/001_CreateTokenTables.sql
```

Or open it in SSMS and execute it. It creates two tables: `UserTokens` and `UserSessions`.

---

## STEP 3: Set up Azure Entra ID redirect URIs

Go to Azure Portal → Entra ID → App Registrations → Your App → Authentication.

Under **Single-page application** platform, add these redirect URIs:

```
https://portal.yourdomain.com/authentication/login-callback
https://learn.yourdomain.com/authentication/login-callback
https://safe.yourdomain.com/authentication/login-callback
https://work.yourdomain.com/authentication/login-callback
https://pulse.yourdomain.com/authentication/login-callback
```

Under **Logout URL** add:

```
https://portal.yourdomain.com/authentication/logout-callback
https://learn.yourdomain.com/authentication/logout-callback
https://safe.yourdomain.com/authentication/logout-callback
https://work.yourdomain.com/authentication/logout-callback
https://pulse.yourdomain.com/authentication/logout-callback
```

---

## STEP 4: Do this for AG ONE PORTAL first (then repeat for each product)

### 4A — Portal UI project (Blazor WASM)

**4A-1.** Add project reference:

```bash
dotnet add AgOne.Portal/UI/AgOne.Portal.UI.csproj reference Shared/AgOne.Shared.Auth/AgOne.Shared.Auth.csproj
```

**4A-2.** Open `AgOne.Portal/UI/Program.cs` and add ONE line:

```csharp
using AgOne.Shared.Auth.Extensions;  // ← add this using

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// ... your existing code ...

builder.Services.AddAgOneSso(builder.Configuration);  // ← ADD THIS LINE

// ... rest of your existing code ...

await builder.Build().RunAsync();
```

**4A-3.** Copy `Step2_AddToEachProduct_UI/Pages/Authentication.razor` into your UI project:

```
Copy to:  AgOne.Portal/UI/Pages/Authentication.razor
```

**4A-4.** Open your `AgOne.Portal/UI/wwwroot/appsettings.json` and MERGE the `AgOneSso` section from:

```
Step2_AddToEachProduct_UI/wwwroot/appsettings.json.PORTAL
```

Replace all `YOUR-TENANT`, `YOUR-PORTAL-CLIENT-ID`, `yourdomain.com` with your real values.

**4A-5.** Open `AgOne.Portal/UI/_Imports.razor` and add these lines at the bottom:

```razor
@using Microsoft.AspNetCore.Authorization
@using Microsoft.AspNetCore.Components.Authorization
@using Microsoft.AspNetCore.Components.WebAssembly.Authentication
@using AgOne.Shared.Auth.Components
@using AgOne.Shared.Auth.Services
```

**4A-6.** Open `AgOne.Portal/UI/App.razor` and change `RouteView` to `AuthorizeRouteView`:

**BEFORE (your current code):**
```razor
<Router AppAssembly="@typeof(App).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
        <FocusOnNavigate RouteData="@routeData" Selector="h1" />
    </Found>
    <NotFound>
        <PageTitle>Not found</PageTitle>
        <LayoutView Layout="@typeof(MainLayout)">
            <p>Sorry, there's nothing at this address.</p>
        </LayoutView>
    </NotFound>
</Router>
```

**AFTER (replace with this):**
```razor
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
                    <p>Loading...</p>
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

**4A-7.** (Optional) Add login display to your `MainLayout.razor`:

```razor
@using AgOne.Shared.Auth.Components

<!-- Add this wherever you want the login/logout button -->
<AgOneLoginDisplay />

<!-- Add this wherever you want product navigation links -->
<AgOneProductNavigator />
```

---

### 4B — Portal API project (.NET Web API)

**4B-1.** Add project reference:

```bash
dotnet add AgOne.Portal/API/AgOne.Portal.API.csproj reference Shared/AgOne.Shared.Auth.Api/AgOne.Shared.Auth.Api.csproj
```

**4B-2.** Open `AgOne.Portal/API/Program.cs` and add these lines:

```csharp
using AgOne.Shared.Auth.Api.Extensions;    // ← add
using AgOne.Shared.Auth.Api.Middleware;     // ← add

var builder = WebApplication.CreateBuilder(args);

// ========================================
// ADD THESE 4 LINES (before builder.Build())
// ========================================
builder.Services.AddAgOneSsoApiAuthentication(builder.Configuration);
builder.Services.AddAgOneSsoApiAuthorization(builder.Configuration);
builder.Services.AddAgOneSsoCors(builder.Configuration);
builder.Services.AddAgOneTokenStorage(builder.Configuration);

// ... your existing services ...

// IMPORTANT: Add this to your existing AddControllers() call:
builder.Services.AddControllers()
    .AddApplicationPart(typeof(AgOne.Shared.Auth.Api.Controllers.AgOneAuthController).Assembly);

var app = builder.Build();

// ... your existing middleware ...

// ========================================
// ADD THESE 5 LINES (before app.MapControllers())
// ========================================
app.UseCors(CorsExtensions.AgOneCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.UseAgOneSsoValidation();
app.UseTokenCapture();

// ... your existing app.MapControllers() etc ...
```

**4B-3.** Open your `AgOne.Portal/API/appsettings.json` and MERGE the `AzureAd`, `AgOneSso`, and `ConnectionStrings` sections from:

```
Step3_AddToEachProduct_API/appsettings.json.PORTAL
```

Replace all `YOUR-TENANT`, `YOUR-TENANT-ID`, `YOUR-PORTAL-API-CLIENT-ID`, etc. with your real values.

**4B-4.** Add `[Authorize]` to your existing controllers:

```csharp
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("api/[controller]")]
[Authorize]                    // ← ADD THIS to require login
public class YourExistingController : ControllerBase
{
    // All your existing code stays the same
    // Now it requires a valid token to access
}
```

---

## STEP 5: Repeat for Learn

Exact same changes as Step 4, but for the Learn project:

### 5A — Learn UI

- **5A-1.** `dotnet add AgOne.Learn/UI/AgOne.Learn.UI.csproj reference Shared/AgOne.Shared.Auth/AgOne.Shared.Auth.csproj`
- **5A-2.** Add `builder.Services.AddAgOneSso(builder.Configuration);` to Learn UI `Program.cs`
- **5A-3.** Copy `Authentication.razor` to `AgOne.Learn/UI/Pages/`
- **5A-4.** Merge `appsettings.json.LEARN` into Learn UI `wwwroot/appsettings.json`
- **5A-5.** Add usings to Learn UI `_Imports.razor`
- **5A-6.** Change `RouteView` to `AuthorizeRouteView` in Learn UI `App.razor`
- **5A-7.** (Optional) Add `<AgOneLoginDisplay />` to Learn's `MainLayout.razor`

### 5B — Learn API

- **5B-1.** `dotnet add AgOne.Learn/API/AgOne.Learn.API.csproj reference Shared/AgOne.Shared.Auth.Api/AgOne.Shared.Auth.Api.csproj`
- **5B-2.** Add the 4 service lines + 5 middleware lines to Learn API `Program.cs`
- **5B-3.** Merge `appsettings.json.LEARN` into Learn API `appsettings.json`
- **5B-4.** Add `[Authorize]` to Learn's controllers

---

## STEP 6: Repeat for Safe, Work, Pulse

Exact same as Step 5. For each product:

| Product | UI appsettings to use | API appsettings to use |
|---------|----------------------|----------------------|
| Safe    | `appsettings.json.SAFE` | `appsettings.json.SAFE` |
| Work    | `appsettings.json.WORK` | `appsettings.json.WORK` |
| Pulse   | `appsettings.json.PULSE` | `appsettings.json.PULSE` |

The code changes (Program.cs, App.razor, _Imports.razor, Authentication.razor) are **IDENTICAL** for every product. Only the `appsettings.json` values are different.

---

## DONE. What works now:

1. User goes to `portal.yourdomain.com` → sees login page → logs in
2. User clicks "Learn" → goes to `learn.yourdomain.com` → **automatically logged in (SSO)**
3. User clicks "Safe" → goes to `safe.yourdomain.com` → **automatically logged in (SSO)**
4. User types `work.yourdomain.com` in browser directly → **redirected to Portal to login first**
5. User clicks Logout → **logged out of ALL products**
6. All tokens saved in database automatically

---

## CHECKLIST: Changes per product

```
For EACH product (Portal, Learn, Safe, Work, Pulse):

UI PROJECT:
  □ Add project reference to AgOne.Shared.Auth
  □ Add 1 line to Program.cs
  □ Add Authentication.razor to Pages/
  □ Merge AgOneSso section into wwwroot/appsettings.json
  □ Add 5 usings to _Imports.razor
  □ Change RouteView → AuthorizeRouteView in App.razor
  □ (Optional) Add AgOneLoginDisplay to MainLayout.razor

API PROJECT:
  □ Add project reference to AgOne.Shared.Auth.Api
  □ Add 4 service lines to Program.cs
  □ Add 5 middleware lines to Program.cs
  □ Add .AddApplicationPart() to AddControllers()
  □ Merge AzureAd + AgOneSso + ConnectionStrings into appsettings.json
  □ Add [Authorize] to controllers

ONLY DIFFERENCE between Portal and other products:
  → Portal:  "RedirectToPortalOnUnauthenticated": false
  → Others:  "RedirectToPortalOnUnauthenticated": true
```

---

## FILE MAP: What this repo contains

```
This Repo/
│
├── STEP-BY-STEP.md                      ← YOU ARE HERE
│
├── Step1_SharedAuthLibraries/           ← Copy these 2 projects into your Shared/ folder
│   ├── AgOne.Shared.Auth/              ← For UI projects (Blazor WASM)
│   │   ├── AgOne.Shared.Auth.csproj
│   │   ├── _Imports.razor
│   │   ├── Configuration/
│   │   │   └── AgOneSsoSettings.cs
│   │   ├── Extensions/
│   │   │   └── ServiceCollectionExtensions.cs
│   │   ├── Handlers/
│   │   │   └── AgOneAuthorizationMessageHandler.cs
│   │   ├── Providers/
│   │   │   └── AgOneAuthStateProvider.cs
│   │   ├── Components/
│   │   │   ├── AgOneAuthGuard.razor + .razor.cs
│   │   │   ├── AgOneRedirectToLogin.razor
│   │   │   ├── AgOneLoginDisplay.razor
│   │   │   ├── AgOneProductNavigator.razor
│   │   │   └── Authentication.razor
│   │   └── Services/
│   │       ├── IAgOneSsoService.cs
│   │       ├── AgOneSsoService.cs
│   │       └── TokenSyncService.cs
│   │
│   └── AgOne.Shared.Auth.Api/          ← For API projects (.NET Web API)
│       ├── AgOne.Shared.Auth.Api.csproj
│       ├── Configuration/
│       │   └── AgOneApiAuthSettings.cs
│       ├── Extensions/
│       │   ├── AuthenticationExtensions.cs
│       │   ├── AuthorizationExtensions.cs
│       │   ├── CorsExtensions.cs
│       │   └── TokenStorageExtensions.cs
│       ├── Middleware/
│       │   ├── AgOneSsoValidationMiddleware.cs
│       │   └── TokenCaptureMiddleware.cs
│       ├── Handlers/
│       │   └── ProductAccessRequirementHandler.cs
│       ├── Controllers/
│       │   └── AgOneAuthController.cs
│       ├── Services/
│       │   ├── ITokenStorageService.cs
│       │   ├── TokenStorageService.cs
│       │   └── TokenCleanupBackgroundService.cs
│       └── Data/
│           ├── AgOneTokenDbContext.cs
│           ├── Entities/
│           │   ├── UserToken.cs
│           │   └── UserSession.cs
│           └── Migrations/
│               └── 001_CreateTokenTables.sql
│
├── Step2_AddToEachProduct_UI/           ← Files to add to each UI project
│   ├── Pages/
│   │   └── Authentication.razor         ← Copy to each UI's Pages/ folder
│   └── wwwroot/
│       ├── appsettings.json.PORTAL      ← Merge into Portal UI appsettings
│       ├── appsettings.json.LEARN       ← Merge into Learn UI appsettings
│       ├── appsettings.json.SAFE        ← Merge into Safe UI appsettings
│       ├── appsettings.json.WORK        ← Merge into Work UI appsettings
│       └── appsettings.json.PULSE       ← Merge into Pulse UI appsettings
│
├── Step3_AddToEachProduct_API/          ← Config to merge into each API project
│   ├── appsettings.json.PORTAL         ← Merge into Portal API appsettings
│   ├── appsettings.json.LEARN          ← Merge into Learn API appsettings
│   ├── appsettings.json.SAFE           ← Merge into Safe API appsettings
│   ├── appsettings.json.WORK           ← Merge into Work API appsettings
│   └── appsettings.json.PULSE          ← Merge into Pulse API appsettings
│
└── Step4_DatabaseSetup/
    └── 001_CreateTokenTables.sql        ← Run once against your database
```
