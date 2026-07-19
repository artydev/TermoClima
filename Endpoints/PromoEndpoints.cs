namespace JsonServer.Endpoints
{
    /// <summary>
    /// Publishing/backup/restore for the Canva-exported wwwroot/promozione.html.
    /// </summary>
    public static class PromoEndpoints
    {
        private const int MaxPromoBackups = 10;

        public static void MapPromoEndpoints(this IEndpointRouteBuilder app)
        {
            var promoGroup = app.MapGroup("/api/promo").RequireAuthorization();

            promoGroup.MapPost("/publish", async (IFormFile file) =>
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

                BackupCurrentPromoPage();
                await File.WriteAllTextAsync(PromoLivePath(), content);

                return Results.Ok(new { published = true });
            }).DisableAntiforgery();

            promoGroup.MapGet("/backups", () =>
            {
                var backupDir = PromoBackupDir();
                if (!Directory.Exists(backupDir)) return Results.Ok(Array.Empty<string>());

                var files = Directory.GetFiles(backupDir, "*.html")
                    .Select(Path.GetFileName)
                    .OrderByDescending(name => name)
                    .ToArray();
                return Results.Ok(files);
            });

            promoGroup.MapGet("/backups/{fileName}", (string fileName) =>
            {
                var safeName = Path.GetFileName(fileName); // strips any path segments — blocks path traversal
                var path = Path.Combine(PromoBackupDir(), safeName);
                return File.Exists(path) ? Results.File(path, "text/html") : Results.NotFound();
            });

            promoGroup.MapPost("/restore/{fileName}", (string fileName) =>
            {
                var safeName = Path.GetFileName(fileName);
                var backupPath = Path.Combine(PromoBackupDir(), safeName);
                if (!File.Exists(backupPath)) return Results.NotFound();

                // Back up whatever's currently live first, so a restore is itself undoable.
                BackupCurrentPromoPage();
                File.Copy(backupPath, PromoLivePath(), overwrite: true);

                return Results.Ok(new { restored = true });
            });
        }

        private static string PromoBackupDir() => Path.Combine(Directory.GetCurrentDirectory(), "PromoBackups");
        private static string PromoLivePath() => Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "promozione.html");

        private static void BackupCurrentPromoPage()
        {
            var livePath = PromoLivePath();
            if (!File.Exists(livePath)) return;

            var backupDir = PromoBackupDir();
            Directory.CreateDirectory(backupDir);
            var backupName = $"promozione_{DateTime.UtcNow:yyyyMMdd_HHmmss}.html";
            File.Copy(livePath, Path.Combine(backupDir, backupName));

            // Keep only the most recent backups so "regular" updates don't grow this folder forever.
            var stale = Directory.GetFiles(backupDir, "*.html").OrderByDescending(f => f).Skip(MaxPromoBackups);
            foreach (var f in stale) File.Delete(f);
        }
    }
}
