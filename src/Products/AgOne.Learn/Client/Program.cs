// ============================================================================
// AG ONE LEARN - Blazor WebAssembly Client Program.cs
// ============================================================================
// This is a PRODUCT application (Learn).
// Copy this EXACT pattern for: Safe, Work, Pulse
// The ONLY difference between products is the appsettings.json configuration.
//
// KEY DIFFERENCE FROM PORTAL:
// - RedirectToPortalOnUnauthenticated = true in appsettings.json
// - This means unauthenticated users get sent to AG ONE Portal first
// ============================================================================

using AgOne.Shared.Auth.Extensions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
// using AgOne.Learn.Client; // <-- Your existing namespace

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// ============================================================================
// Your existing root component registrations (keep these as-is)
// ============================================================================
// builder.RootComponents.Add<App>("#app");
// builder.RootComponents.Add<HeadOutlet>("head::after");

// ============================================================================
// AG ONE SSO REGISTRATION - SAME single line as Portal
// (Configuration in appsettings.json determines product behavior)
// ============================================================================
builder.Services.AddAgOneSso(builder.Configuration);

// ============================================================================
// Your existing service registrations (keep these as-is)
// ============================================================================
// builder.Services.AddScoped<ILearnService, LearnService>();
// builder.Services.AddScoped<ICourseService, CourseService>();
// ... etc ...

await builder.Build().RunAsync();
