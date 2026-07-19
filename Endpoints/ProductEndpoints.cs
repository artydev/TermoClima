using JsonServer.Data;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace JsonServer.Endpoints
{
    /// <summary>
    /// Dynamic CRUD routes — one set per table found in db.json at startup
    /// (e.g. "products"). New collections aren't creatable at runtime; they
    /// need a matching top-level key in db.json before the app starts.
    ///
    /// These routes have no authorization at all, by design — fine for a
    /// public product catalog, wrong for anything containing customer PII.
    /// Pass table names to exclude (e.g. "orders") so they can get their own
    /// endpoints elsewhere with the right auth split instead.
    /// </summary>
    public static class ProductEndpoints
    {
        public static void MapProductEndpoints(this IEndpointRouteBuilder app, Database database, params string[] excludeTables)
        {
            var excluded = new HashSet<string>(excludeTables, StringComparer.OrdinalIgnoreCase);

            foreach (var table in database.TableNames)
            {
                if (excluded.Contains(table)) continue;

                app.MapGet($"/{table}", (Database db) =>
                    Results.Json(db.Tables.GetValueOrDefault(table) ?? (object)new { }));

                app.MapGet($"/{table}/{{id}}", (Database db, string id) =>
                    db.GetById(id, table) is { } item ? Results.Json(item) : Results.NotFound());

                app.MapPost($"/{table}", (Database db, [FromBody] JsonElement body) =>
                    Results.Created($"/{table}", db.Insert(body, table)));

                app.MapPut($"/{table}/{{id}}", (Database db, string id, [FromBody] JsonElement body) =>
                    db.Update(id, body, table) is { } item ? Results.Json(item) : Results.NotFound());

                app.MapDelete($"/{table}/{{id}}", (Database db, string id) =>
                    db.Delete(id, table) ? Results.NoContent() : Results.NotFound());
            }
        }
    }
}
