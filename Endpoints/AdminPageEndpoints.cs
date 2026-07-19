namespace JsonServer.Endpoints
{
    /// <summary>
    /// Serves the SecureAdmin/*.html pages, gated behind the same cookie
    /// authentication as the admin APIs (/api/promo). Note /api/media is
    /// NOT currently behind RequireAuthorization() — a pre-existing gap,
    /// unchanged by this refactor.
    /// </summary>
    public static class AdminPageEndpoints
    {
        public static void MapAdminPageEndpoints(this IEndpointRouteBuilder app)
        {
            var adminGroup = app.MapGroup("/admin").RequireAuthorization();

            adminGroup.MapGet("/dashboard", (HttpContext ctx) => ServeAdminPage(ctx, "dashboard.html"));
            adminGroup.MapGet("/images", (HttpContext ctx) => ServeAdminPage(ctx, "image-manager.html"));
            adminGroup.MapGet("/promo", (HttpContext ctx) => ServeAdminPage(ctx, "publish-promo.html"));
            adminGroup.MapGet("/orders", (HttpContext ctx) => ServeAdminPage(ctx, "orders.html"));
        }

        // These pages are served via Results.File(), which bypasses UseStaticFiles()
        // entirely — so without this, they'd get no Cache-Control header at all and
        // be left to browser heuristics, the same staleness risk the CSS/JS caching
        // policy exists to avoid. Always revalidate, same as any other HTML page.
        private static IResult ServeAdminPage(HttpContext ctx, string fileName)
        {
            ctx.Response.Headers["Cache-Control"] = "no-cache, must-revalidate";
            return Results.File(Path.Combine(Directory.GetCurrentDirectory(), "SecureAdmin", fileName), "text/html");
        }
    }
}
