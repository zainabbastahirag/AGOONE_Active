// ============================================================================
// AG ONE PORTAL - Blazor WebAssembly Client Program.cs
// ============================================================================
// This is the MAIN PORTAL (hub) application.
// Copy this pattern into your existing AG ONE Portal's Program.cs.
//
// The Portal is the primary login point. Users authenticate here first,
// then navigate to other products (Learn, Safe, Work, Pulse) with SSO.
// ============================================================================

using AgOne.Shared.Auth.Extensions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
// using AgOne.Portal.Client; // <-- Your existing namespace

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// ============================================================================
// Your existing root component registrations (keep these as-is)
// ============================================================================
// builder.RootComponents.Add<App>("#app");
// builder.RootComponents.Add<HeadOutlet>("head::after");

// ============================================================================
// AG ONE SSO REGISTRATION - Add this single line
// ============================================================================
builder.Services.AddAgOneSso(builder.Configuration);

// ============================================================================
// Your existing service registrations (keep these as-is)
// Example:
// builder.Services.AddScoped<IYourService, YourService>();
// ============================================================================

// ============================================================================
// OPTIONAL: If the Portal needs to call other product APIs directly
// ============================================================================
// builder.Services.AddAgOneProductApiClient(
//     "AgOne.LearnApi",
//     "https://learn-api.agone.com",
//     "api://learn-api-client-id/access_as_user");

await builder.Build().RunAsync();
