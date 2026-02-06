using System.IdentityModel.Tokens.Jwt;
using AgOne.Shared.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AgOne.API.Auth;

/// <summary>
/// Captures Bearer tokens from every API request and saves them to DB.
/// Usage: app.UseTokenCapture();
/// </summary>
public class TokenCaptureMiddleware
{
    private readonly RequestDelegate _next;

    public TokenCaptureMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            try
            {
                var auth = context.Request.Headers.Authorization.ToString();
                if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    var accessToken = auth["Bearer ".Length..].Trim();
                    var userId = context.User.FindFirst("oid")?.Value ?? context.User.FindFirst("sub")?.Value;

                    if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(accessToken))
                    {
                        var settings = context.RequestServices.GetRequiredService<IOptions<AgOneApiAuthSettings>>().Value;
                        var tokenStorage = context.RequestServices.GetService<ITokenStorageService>();

                        if (tokenStorage != null)
                        {
                            DateTime expiresUtc;
                            try { expiresUtc = new JwtSecurityTokenHandler().ReadJwtToken(accessToken).ValidTo; }
                            catch { expiresUtc = DateTime.UtcNow.AddHours(1); }

                            await tokenStorage.SaveTokenAsync(new SaveTokenRequest
                            {
                                UserId = userId,
                                Email = context.User.FindFirst("preferred_username")?.Value,
                                DisplayName = context.User.FindFirst("name")?.Value,
                                ProductName = settings.ProductName,
                                AccessToken = accessToken,
                                AccessTokenExpiresUtc = expiresUtc,
                                GrantedScopes = context.User.FindFirst("scp")?.Value,
                                TenantId = context.User.FindFirst("tid")?.Value,
                                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                                UserAgent = context.Request.Headers.UserAgent.ToString()
                            });

                            await tokenStorage.TrackSessionAsync(new TrackSessionRequest
                            {
                                UserId = userId,
                                Email = context.User.FindFirst("preferred_username")?.Value,
                                ProductName = settings.ProductName,
                                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                                UserAgent = context.Request.Headers.UserAgent.ToString()
                            });
                        }
                    }
                }
            }
            catch { /* token capture must never break the request */ }
        }

        await _next(context);
    }
}

public static class TokenCaptureMiddlewareExtensions
{
    public static IApplicationBuilder UseTokenCapture(this IApplicationBuilder builder)
        => builder.UseMiddleware<TokenCaptureMiddleware>();
}
