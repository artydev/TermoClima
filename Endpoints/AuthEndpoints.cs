using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace JsonServer.Endpoints
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/login", (HttpContext ctx) =>
            {
                ctx.Response.Headers["Cache-Control"] = "no-cache, must-revalidate";
                return Results.File(Path.Combine(Directory.GetCurrentDirectory(), "SecureAdmin", "login.html"), "text/html");
            });

            app.MapPost("/login", async (HttpContext ctx, IOptions<AdminSettings> adminSettings) =>
            {
                var configuredPassword = adminSettings.Value.Password;
                var submitted = ctx.Request.Form["password"].ToString();

                if (!string.IsNullOrEmpty(configuredPassword) && PasswordsMatch(submitted, configuredPassword))
                {
                    var claims = new[] { new Claim(ClaimTypes.Name, "Admin") };
                    await ctx.SignInAsync("AdminCookie", new ClaimsPrincipal(new ClaimsIdentity(claims, "AdminCookie")));

                    var returnUrl = ctx.Request.Form["ReturnUrl"].ToString();
                    return Results.Redirect(!string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith("/") ? returnUrl : "/admin/dashboard");
                }
                return Results.Unauthorized();
            }).RequireRateLimiting("login");
        }

        // Constant-time comparison (hashing first so lengths are equal and don't leak).
        private static bool PasswordsMatch(string submitted, string expected) =>
            CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(Encoding.UTF8.GetBytes(submitted)),
                SHA256.HashData(Encoding.UTF8.GetBytes(expected)));
    }
}
