// ============================================================================
// AG ONE PORTAL - .NET API Backend Program.cs
// ============================================================================
// This is the MAIN PORTAL API backend.
// Copy this pattern into your existing AG ONE Portal API's Program.cs.
// ============================================================================

using AgOne.Shared.Auth.Api.Extensions;
using AgOne.Shared.Auth.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// AG ONE SSO AUTHENTICATION & AUTHORIZATION - Add these 3 lines
// ============================================================================
builder.Services.AddAgOneSsoApiAuthentication(builder.Configuration);
builder.Services.AddAgOneSsoApiAuthorization(builder.Configuration);
builder.Services.AddAgOneSsoCors(builder.Configuration);

// ============================================================================
// Your existing service registrations (keep these as-is)
// ============================================================================
builder.Services.AddControllers();
// builder.Services.AddScoped<IYourRepository, YourRepository>();
// ... your other services ...

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ============================================================================
// MIDDLEWARE PIPELINE
// ============================================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ============================================================================
// AG ONE SSO MIDDLEWARE - Add these 4 lines in this order
// ============================================================================
app.UseCors(CorsExtensions.AgOneCorsPolicy);    // 1. CORS first
app.UseAuthentication();                          // 2. Authentication
app.UseAuthorization();                           // 3. Authorization
app.UseAgOneSsoValidation();                      // 4. AG ONE SSO validation

// ============================================================================
// Your existing middleware and endpoints (keep these as-is)
// ============================================================================
app.MapControllers();

// ============================================================================
// OPTIONAL: Blazor WebAssembly hosting (if your API serves the WASM client)
// ============================================================================
// app.UseBlazorFrameworkFiles();
// app.UseStaticFiles();
// app.MapFallbackToFile("index.html");

app.Run();
