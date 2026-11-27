namespace JsonServer.Endpoints
{
    /// <summary>
    /// Publishing/backup/restore for a standalone HTML page in wwwroot
    /// (e.g. a Canva export). One instance per page: promozione, assistenza.
    /// All routes require the admin cookie.
    /// </summary>
    public static class PagePublisherEndpoints
    {
        private const int MaxBackups = 10;

        /// <param name="routePrefix">e.g. "/api/promo"</param>
        /// <param name="liveFileName">file in wwwroot, e.g. "promozione.html"</param>
        /// <param name="backupFolder">folder next to the app, e.g. "PromoBackups"</param>
        /// <param name="backupPrefix">backup file name prefix, e.g. "promozione"</param>
        public static void MapPagePublisher(this IEndpointRouteBuilder app,
            string routePrefix, string liveFileName, string backupFolder, string backupPrefix)
        {
            string BackupDir() => Path.Combine(Directory.GetCurrentDirectory(), backupFolder);
            string LivePath() => Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", liveFileName);

            void BackupCurrent()
            {
                var livePath = LivePath();
                if (!File.Exists(livePath)) return;

                var backupDir = BackupDir();
                Directory.CreateDirectory(backupDir);
                var backupName = $"{backupPrefix}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.html";
                File.Copy(livePath, Path.Combine(backupDir, backupName));

                // Keep only the most recent backups so regular updates don't grow this folder forever.
                var stale = Directory.GetFiles(backupDir, "*.html").OrderByDescending(f => f).Skip(MaxBackups);
                foreach (var f in stale) File.Delete(f);
            }

            var group = app.MapGroup(routePrefix).RequireAuthorization();

            group.MapPost("/publish", async (IFormFile file) =>
            {
                if (file is null || file.Length == 0)
                    return Results.BadRequest(new { error = "Nessun file ricevuto." });

                var ext = Path.GetExtension(file.FileName);
                if (!string.Equals(ext, ".html", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(ext, ".htm", StringComparison.OrdinalIgnoreCase))
                    return Results.BadRequest(new { error = "Il file deve essere un .html o .htm." });

                string content;
                using (var reader = new StreamReader(file.OpenReadStream()))
                    content = await reader.ReadToEndAsync();

                if (!content.Contains("<html", StringComparison.OrdinalIgnoreCase))
                    return Results.BadRequest(new { error = "Il file non sembra una pagina HTML valida." });

                BackupCurrent();
                await File.WriteAllTextAsync(LivePath(), content);

                return Results.Ok(new { published = true });
            }).DisableAntiforgery();

            group.MapGet("/backups", () =>
            {
                var backupDir = BackupDir();
                if (!Directory.Exists(backupDir)) return Results.Ok(Array.Empty<string>());

                var files = Directory.GetFiles(backupDir, "*.html")
                    .Select(Path.GetFileName)
                    .OrderByDescending(name => name)
                    .ToArray();
                return Results.Ok(files);
            });

            group.MapGet("/backups/{fileName}", (string fileName) =>
            {
                var safeName = Path.GetFileName(fileName); // strips any path segments — blocks path traversal
                var path = Path.Combine(BackupDir(), safeName);
                return File.Exists(path) ? Results.File(path, "text/html") : Results.NotFound();
            });

            group.MapPost("/restore/{fileName}", (string fileName) =>
            {
                var safeName = Path.GetFileName(fileName);
                var backupPath = Path.Combine(BackupDir(), safeName);
                if (!File.Exists(backupPath)) return Results.NotFound();

                // Back up whatever's currently live first, so a restore is itself undoable.
                BackupCurrent();
                File.Copy(backupPath, LivePath(), overwrite: true);

                return Results.Ok(new { restored = true });
            });
        }
    }
}
