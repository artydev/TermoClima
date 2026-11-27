namespace JsonServer.Endpoints
{
    /// <summary>
    /// Image upload/list/delete for the product image manager.
    /// NOTE: not currently behind RequireAuthorization() — a pre-existing
    /// gap carried over unchanged from before this refactor.
    /// </summary>
    public static class MediaEndpoints
    {
        private static string UploadsDir() =>
            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".webp", ".png", ".jpg", ".jpeg", ".gif", ".svg" };

        public static void MapMediaEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGroup("/api/media")
               .MapPost("/upload", async (IFormFile file) =>
               {
                   Directory.CreateDirectory(UploadsDir());
                   // Strip any directory part (path traversal) and only accept images.
                   var name = Path.GetFileName(file.FileName);
                   if (string.IsNullOrEmpty(name) || !AllowedExtensions.Contains(Path.GetExtension(name)))
                       return Results.BadRequest("Only image files are allowed.");
                   var path = Path.Combine(UploadsDir(), name);
                   using var stream = new FileStream(path, FileMode.Create);
                   await file.CopyToAsync(stream);
                   return Results.Ok();
               }).DisableAntiforgery().RequireAuthorization();

            app.MapDelete("/api/media/delete/{fileName}", (string fileName) =>
            {
                var path = Path.Combine(UploadsDir(), Path.GetFileName(fileName));
                if (File.Exists(path)) { File.Delete(path); return Results.NoContent(); }
                return Results.NotFound();
            }).RequireAuthorization();

            app.MapGet("/api/images", () =>
            {
                Directory.CreateDirectory(UploadsDir());
                return Results.Ok(Directory.GetFiles(UploadsDir()).Select(Path.GetFileName));
            });
        }
    }
}
