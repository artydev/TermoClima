using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;

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

                if (!string.IsNullOrEmpty(configuredPassword) && submitted == configuredPassword)
                {
                    var claims = new[] { new Claim(ClaimTypes.Name, "Admin") };
                    await ctx.SignInAsync("AdminCookie", new ClaimsPrincipal(new ClaimsIdentity(claims, "AdminCookie")));

                    var returnUrl = ctx.Request.Form["ReturnUrl"].ToString();
                    return Results.Redirect(!string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith("/") ? returnUrl : "/admin/dashboard");
                }
                return Results.Unauthorized();
            });
        }
    }
}
