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

        public static void MapMediaEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGroup("/api/media")
               .MapPost("/upload", async (IFormFile file) =>
               {
                   Directory.CreateDirectory(UploadsDir());
                   var path = Path.Combine(UploadsDir(), file.FileName);
                   using var stream = new FileStream(path, FileMode.Create);
                   await file.CopyToAsync(stream);
                   return Results.Ok();
               }).DisableAntiforgery();

            app.MapDelete("/api/media/delete/{fileName}", (string fileName) =>
            {
                var path = Path.Combine(UploadsDir(), fileName);
                if (File.Exists(path)) { File.Delete(path); return Results.NoContent(); }
                return Results.NotFound();
            });

            app.MapGet("/api/images", () =>
            {
                Directory.CreateDirectory(UploadsDir());
                return Results.Ok(Directory.GetFiles(UploadsDir()).Select(Path.GetFileName));
            });
        }
    }
}
