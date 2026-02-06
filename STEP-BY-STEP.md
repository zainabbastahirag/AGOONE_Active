# AG ONE SSO — Straight Step-by-Step

Your existing solution:

```
YourSolution/
├── AgOne.Shared/           ← existing class library (global, used by all products)
│
├── AgOne.Portal/
│   ├── UI/                 ← existing Blazor WASM
│   ├── API/                ← existing .NET Web API
│   ├── Infrastructure/     ← existing class library
│   ├── Domain/             ← existing class library
│   └── Shared/             ← existing class library
│
├── AgOne.Learn/            ← same structure
├── AgOne.Safe/             ← same structure
├── AgOne.Work/             ← same structure
└── AgOne.Pulse/            ← same structure
```

No new projects. Files go INTO your existing projects.

---

## Step 1 — Add 4 files to AgOne.Shared (your existing global shared project)

Copy the `Auth/` folder from `AddTo_AgOneShared/` into your `AgOne.Shared/` project:

```
AgOne.Shared/
└── Auth/                          ← NEW folder
    ├── AgOneSsoSettings.cs        ← copy from AddTo_AgOneShared/Auth/
    ├── AgOneApiAuthSettings.cs    ← copy from AddTo_AgOneShared/Auth/
    ├── IAgOneSsoService.cs        ← copy from AddTo_AgOneShared/Auth/
    └── ITokenStorageService.cs    ← copy from AddTo_AgOneShared/Auth/
```

**Fix namespaces:** Open each file and change `namespace AgOne.Shared.Auth` to match your actual namespace if different.

No NuGet packages needed. These are plain C# files.

---

## Step 2 — Add to AG ONE Portal (do Portal first, then repeat for others)

### 2A — Portal Infrastructure project

Copy `Auth/` folder from `AddTo_EachProduct_Infrastructure/` into your `AgOne.Portal/Infrastructure/`:

```
AgOne.Portal/Infrastructure/
└── Auth/                              ← NEW folder
    ├── Entities/
    │   ├── UserToken.cs               ← copy
    │   └── UserSession.cs             ← copy
    ├── UserTokenConfiguration.cs      ← copy (EF Fluent API config)
    ├── UserSessionConfiguration.cs    ← copy (EF Fluent API config)
    └── TokenStorageService.cs         ← copy
```

**Fix namespaces** to match your project.

**Add NuGet packages** to Infrastructure .csproj:
```bash
dotnet add AgOne.Portal/Infrastructure/ package Microsoft.EntityFrameworkCore
dotnet add AgOne.Portal/Infrastructure/ package System.IdentityModel.Tokens.Jwt
```

**Open your existing DbContext** and add these 2 DbSet properties:
```csharp
using AgOne.Infrastructure.Auth.Entities;  // ← adjust to your namespace

public class YourAppDbContext : DbContext
{
    // ... your existing DbSets ...

    // ADD THESE 2 LINES:
    public DbSet<UserToken> UserTokens => Set<UserToken>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ... your existing configurations ...

        // ADD THESE 2 LINES:
        modelBuilder.ApplyConfiguration(new UserTokenConfiguration());
        modelBuilder.ApplyConfiguration(new UserSessionConfiguration());
    }
}
```

**Open `TokenStorageService.cs`** and change `DbContext` to your actual type:
```csharp
// Change this:
public TokenStorageService(DbContext db, ...
// To your actual DbContext name:
public TokenStorageService(YourAppDbContext db, ...
```

And change the field type at the top:
```csharp
// Change this:
private readonly DbContext _db;
// To:
private readonly YourAppDbContext _db;
```

**Register in your DI** (in `Program.cs` or wherever you register Infrastructure services):
```csharp
builder.Services.AddScoped<ITokenStorageService, TokenStorageService>();
```

### 2B — Create EF Core Migration

Now run the migration command from your API project directory (the project that has the EF tools and startup):

```bash
dotnet ef migrations add AddAgOneSsoTokenTables --project AgOne.Portal/Infrastructure/ --startup-project AgOne.Portal/API/
```

Then apply it:

```bash
dotnet ef database update --project AgOne.Portal/Infrastructure/ --startup-project AgOne.Portal/API/
```

This creates the `UserTokens` and `UserSessions` tables with all indexes automatically.

If you want to verify what it will generate before applying, run:

```bash
dotnet ef migrations script --project AgOne.Portal/Infrastructure/ --startup-project AgOne.Portal/API/
```

---

### 2C — Portal API project

Copy `Auth/` folder from `AddTo_EachProduct_API/` into your `AgOne.Portal/API/`:

```
AgOne.Portal/API/
└── Auth/                                      ← NEW folder
    ├── SsoApiServiceCollectionExtensions.cs    ← copy
    ├── TokenCaptureMiddleware.cs               ← copy
    └── AgOneAuthController.cs                  ← copy
```

**Fix namespaces** to match your project.

**Add NuGet packages** to API .csproj:
```bash
dotnet add AgOne.Portal/API/ package Microsoft.Identity.Web
dotnet add AgOne.Portal/API/ package System.IdentityModel.Tokens.Jwt
```

**Open `AgOne.Portal/API/Program.cs`** and add these lines:

```csharp
using AgOne.API.Auth;  // ← adjust to your namespace

// ──────────── ADD BEFORE builder.Build() ────────────
builder.Services.AddAgOneSsoApi(builder.Configuration);

// ──────────── ADD BEFORE app.MapControllers() ────────────
app.UseCors("AgOneCors");
app.UseAuthentication();
app.UseAuthorization();
app.UseTokenCapture();
```

**Merge config** into `AgOne.Portal/API/appsettings.json`:

Copy the `AzureAd`, `AgOneSso`, and `ConnectionStrings` sections from `Appsettings/API/AllProducts.json` into your existing appsettings. Set `"ProductName": "Portal"` and use Portal's API ClientId.

---

### 2D — Portal UI project

Copy files from `AddTo_EachProduct_UI/` into your `AgOne.Portal/UI/`:

```
AgOne.Portal/UI/
├── Auth/                                       ← NEW folder
│   ├── SsoServiceCollectionExtensions.cs       ← copy
│   ├── AgOneSsoService.cs                      ← copy
│   ├── AgOneAuthorizationMessageHandler.cs     ← copy
│   ├── AgOneRedirectToLogin.razor              ← copy
│   └── AgOneLoginDisplay.razor                 ← copy
└── Pages/
    └── Authentication.razor                    ← NEW file, copy
```

**Fix namespaces** to match your project.

**Add NuGet packages** to UI .csproj:
```bash
dotnet add AgOne.Portal/UI/ package Microsoft.Authentication.WebAssembly.Msal
dotnet add AgOne.Portal/UI/ package Microsoft.AspNetCore.Components.WebAssembly.Authentication
```

**Open `AgOne.Portal/UI/Program.cs`** and add 1 line:

```csharp
using AgOne.UI.Auth;  // ← adjust to your namespace

// ADD THIS ONE LINE (anywhere before builder.Build().RunAsync())
builder.Services.AddAgOneSso(builder.Configuration);
```

**Open `AgOne.Portal/UI/_Imports.razor`** and add:

```razor
@using Microsoft.AspNetCore.Authorization
@using Microsoft.AspNetCore.Components.Authorization
@using Microsoft.AspNetCore.Components.WebAssembly.Authentication
```

**Open `AgOne.Portal/UI/App.razor`** and change:

FROM:
```razor
<RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
```

TO:
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
                        <p>Not authorized.</p>
                    }
                </NotAuthorized>
                <Authorizing>
                    <p>Loading...</p>
                </Authorizing>
            </AuthorizeRouteView>
            <FocusOnNavigate RouteData="@routeData" Selector="h1" />
        </Found>
        <NotFound>
            <LayoutView Layout="@typeof(MainLayout)">
                <p>Not found.</p>
            </LayoutView>
        </NotFound>
    </Router>
</CascadingAuthenticationState>
```

**Merge config** into `AgOne.Portal/UI/wwwroot/appsettings.json`:

Copy the `AgOneSso` section from `Appsettings/UI/Portal.json`. Replace all YOUR- placeholders with real values.

**Add `[Authorize]` to your existing API controllers.**

---

## Step 3 — Repeat Step 2 for Learn

**Exact same files.** Copy the same `Auth/` folders to:
- `AgOne.Learn/Infrastructure/Auth/`
- `AgOne.Learn/API/Auth/`
- `AgOne.Learn/UI/Auth/` + `Pages/Authentication.razor`

Same code changes to Program.cs, App.razor, _Imports.razor, DbContext.

**Run migration for Learn too:**
```bash
dotnet ef migrations add AddAgOneSsoTokenTables --project AgOne.Learn/Infrastructure/ --startup-project AgOne.Learn/API/
dotnet ef database update --project AgOne.Learn/Infrastructure/ --startup-project AgOne.Learn/API/
```

**Only config difference:** Use `Appsettings/UI/Learn.json` (which has `"RedirectToPortalOnUnauthenticated": true`).

---

## Step 4 — Repeat for Safe, Work, Pulse

Exact same. Use the matching appsettings from `Appsettings/UI/Safe.json`, `Work.json`, `Pulse.json`.

Run migration for each:
```bash
dotnet ef migrations add AddAgOneSsoTokenTables --project AgOne.Safe/Infrastructure/ --startup-project AgOne.Safe/API/
dotnet ef database update --project AgOne.Safe/Infrastructure/ --startup-project AgOne.Safe/API/

dotnet ef migrations add AddAgOneSsoTokenTables --project AgOne.Work/Infrastructure/ --startup-project AgOne.Work/API/
dotnet ef database update --project AgOne.Work/Infrastructure/ --startup-project AgOne.Work/API/

dotnet ef migrations add AddAgOneSsoTokenTables --project AgOne.Pulse/Infrastructure/ --startup-project AgOne.Pulse/API/
dotnet ef database update --project AgOne.Pulse/Infrastructure/ --startup-project AgOne.Pulse/API/
```

NOTE: If all products share the SAME database, you only need to run the migration ONCE (from Portal). The tables are the same for all products.

---

## Summary: What you add to each project

```
AgOne.Shared/                          ← ADD ONCE (shared by all)
└── Auth/  (4 files: settings, interfaces, DTOs)

For EACH product (Portal, Learn, Safe, Work, Pulse):

  Infrastructure/                      ← ADD Auth/ folder
  └── Auth/
      ├── Entities/UserToken.cs
      ├── Entities/UserSession.cs
      ├── UserTokenConfiguration.cs      (EF Fluent API)
      ├── UserSessionConfiguration.cs    (EF Fluent API)
      └── TokenStorageService.cs
      + Add DbSets to your existing DbContext
      + Add ApplyConfiguration() to OnModelCreating
      + Register ITokenStorageService in DI
      + Run: dotnet ef migrations add AddAgOneSsoTokenTables
      + Run: dotnet ef database update
      + NuGet: Microsoft.EntityFrameworkCore, System.IdentityModel.Tokens.Jwt

  API/                                 ← ADD Auth/ folder
  └── Auth/
      ├── SsoApiServiceCollectionExtensions.cs
      ├── TokenCaptureMiddleware.cs
      └── AgOneAuthController.cs
      + Add 1 line to Program.cs: builder.Services.AddAgOneSsoApi(...)
      + Add 4 middleware lines to Program.cs
      + Merge AzureAd + AgOneSso into appsettings.json
      + Add [Authorize] to controllers
      + NuGet: Microsoft.Identity.Web, System.IdentityModel.Tokens.Jwt

  UI/                                  ← ADD Auth/ folder + 1 page
  ├── Auth/
  │   ├── SsoServiceCollectionExtensions.cs
  │   ├── AgOneSsoService.cs
  │   ├── AgOneAuthorizationMessageHandler.cs
  │   ├── AgOneRedirectToLogin.razor
  │   └── AgOneLoginDisplay.razor
  └── Pages/Authentication.razor
      + Add 1 line to Program.cs: builder.Services.AddAgOneSso(...)
      + Add 3 @using to _Imports.razor
      + Change RouteView → AuthorizeRouteView in App.razor
      + Merge AgOneSso into wwwroot/appsettings.json
      + NuGet: Microsoft.Authentication.WebAssembly.Msal

ONLY config difference:
  Portal → "RedirectToPortalOnUnauthenticated": false
  Learn/Safe/Work/Pulse → "RedirectToPortalOnUnauthenticated": true
```
