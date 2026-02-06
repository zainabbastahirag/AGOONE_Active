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
// AG ONE SSO AUTHENTICATION & AUTHORIZATION - Add these lines
// ============================================================================
builder.Services.AddAgOneSsoApiAuthentication(builder.Configuration);
builder.Services.AddAgOneSsoApiAuthorization(builder.Configuration);
builder.Services.AddAgOneSsoCors(builder.Configuration);

// ============================================================================
// AG ONE TOKEN STORAGE (DB) - Add these lines to enable token persistence
// ============================================================================
// Option A: Use a dedicated AgOneTokenDbContext (separate DB or schema)
builder.Services.AddAgOneTokenStorage(builder.Configuration);

// Option B: Use your existing DbContext (uncomment and replace YourDbContext)
// builder.Services.AddAgOneTokenStorage<YourDbContext>(builder.Configuration);

// Optional: Background cleanup of expired tokens/sessions
builder.Services.AddAgOneTokenCleanup();

// ============================================================================
// Your existing service registrations (keep these as-is)
// ============================================================================
builder.Services.AddControllers()
    // IMPORTANT: Add this to discover the AgOneAuthController from the shared library
    .AddApplicationPart(typeof(AgOne.Shared.Auth.Api.Controllers.AgOneAuthController).Assembly);

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
// AG ONE SSO MIDDLEWARE - In this order
// ============================================================================
app.UseCors(CorsExtensions.AgOneCorsPolicy);    // 1. CORS first
app.UseAuthentication();                          // 2. Authentication
app.UseAuthorization();                           // 3. Authorization
app.UseAgOneSsoValidation();                      // 4. AG ONE SSO validation
app.UseTokenCapture();                            // 5. Token capture (saves to DB)

// ============================================================================
// Your existing middleware and endpoints (keep these as-is)
// ============================================================================
app.MapControllers();

// OPTIONAL: Blazor WebAssembly hosting
// app.UseBlazorFrameworkFiles();
// app.UseStaticFiles();
// app.MapFallbackToFile("index.html");

app.Run();
