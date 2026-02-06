// ============================================================================
// AG ONE LEARN - .NET API Backend Program.cs
// ============================================================================
// This is a PRODUCT API backend (Learn).
// Copy this EXACT pattern for: Safe, Work, Pulse
// The ONLY difference between products is the appsettings.json configuration.
// ============================================================================

using AgOne.Shared.Auth.Api.Extensions;
using AgOne.Shared.Auth.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// AG ONE SSO AUTHENTICATION & AUTHORIZATION - SAME lines as Portal
// ============================================================================
builder.Services.AddAgOneSsoApiAuthentication(builder.Configuration);
builder.Services.AddAgOneSsoApiAuthorization(builder.Configuration);
builder.Services.AddAgOneSsoCors(builder.Configuration);

// ============================================================================
// AG ONE TOKEN STORAGE (DB) - Enable token persistence
// ============================================================================
builder.Services.AddAgOneTokenStorage(builder.Configuration);
builder.Services.AddAgOneTokenCleanup();

// ============================================================================
// Your existing service registrations (keep these as-is)
// ============================================================================
builder.Services.AddControllers()
    .AddApplicationPart(typeof(AgOne.Shared.Auth.Api.Controllers.AgOneAuthController).Assembly);

// builder.Services.AddScoped<ICourseRepository, CourseRepository>();
// builder.Services.AddScoped<ILessonService, LessonService>();
// ... your other services ...

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ============================================================================
// AG ONE SSO MIDDLEWARE - SAME order as Portal
// ============================================================================
app.UseCors(CorsExtensions.AgOneCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.UseAgOneSsoValidation();
app.UseTokenCapture();                            // Saves tokens to DB

// ============================================================================
// Your existing middleware and endpoints (keep these as-is)
// ============================================================================
app.MapControllers();

// OPTIONAL: Blazor WASM hosting
// app.UseBlazorFrameworkFiles();
// app.UseStaticFiles();
// app.MapFallbackToFile("index.html");

app.Run();
