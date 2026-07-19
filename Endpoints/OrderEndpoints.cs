using JsonServer.Data;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace JsonServer.Endpoints
{
    /// <summary>
    /// Orders are deliberately NOT part of the generic dynamic-table CRUD
    /// (see ProductEndpoints' excludeTables) — an order carries customer PII
    /// (name, address, phone), so reading orders needs to require admin
    /// auth, while creating one must NOT (a customer submitting their own
    /// order isn't logged in). That split doesn't fit the generic per-table
    /// loop, so orders get their own routes here, reusing the same
    /// Database storage underneath.
    /// </summary>
    public static class OrderEndpoints
    {
        private const string Table = "orders";

        public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
        {
            // Public — a customer submitting their own order via checkout.
            app.MapPost("/api/orders", async (Database db, EmailService email, OrderRequest request) =>
            {
                if (string.IsNullOrWhiteSpace(request.CustomerName))
                    return Results.BadRequest(new { error = "Nome obbligatorio." });
                if (string.IsNullOrWhiteSpace(request.Email))
                    return Results.BadRequest(new { error = "Email obbligatoria." });
                if (request.Items is null || request.Items.Count == 0)
                    return Results.BadRequest(new { error = "Il carrello è vuoto." });

                var orderDict = new Dictionary<string, object>
                {
                    ["customerName"] = request.CustomerName,
                    ["email"] = request.Email,
                    ["phone"] = request.Phone ?? "",
                    ["address"] = request.Address ?? "",
                    ["city"] = request.City ?? "",
                    ["postalCode"] = request.PostalCode ?? "",
                    ["deliveryMethod"] = request.DeliveryMethod ?? "",
                    ["notes"] = request.Notes ?? "",
                    ["items"] = request.Items,
                    ["subtotal"] = request.Subtotal,
                    ["status"] = "In attesa",
                    ["createdAt"] = DateTime.UtcNow.ToString("o"),
                };

                var element = JsonSerializer.SerializeToElement(orderDict);
                var order = db.Insert(element, Table);
                if (order is null)
                    return Results.Problem("Impossibile salvare l'ordine.", statusCode: StatusCodes.Status500InternalServerError);

                // Best-effort: the order is already saved even if the notification email fails,
                // so a flaky SMTP send shouldn't make the customer think their order was lost.
                try
                {
                    await email.SendOrderConfirmationAsync(order);
                }
                catch
                {
                    // swallow — order persisted regardless; admin can still see it in /api/orders
                }

                return Results.Created($"/api/orders/{order["id"]}", order);
            });

            // Admin-only from here — orders contain customer PII.
            var adminOrders = app.MapGroup("/api/orders").RequireAuthorization();

            adminOrders.MapGet("/", (Database db) =>
                Results.Json(db.Tables.GetValueOrDefault(Table) ?? (object)new List<object>()));

            adminOrders.MapGet("/{id}", (Database db, string id) =>
                db.GetById(id, Table) is { } order ? Results.Json(order) : Results.NotFound());

            adminOrders.MapPut("/{id}", (Database db, string id, [FromBody] JsonElement body) =>
                db.Update(id, body, Table) is { } order ? Results.Json(order) : Results.NotFound());

            adminOrders.MapDelete("/{id}", (Database db, string id) =>
                db.Delete(id, Table) ? Results.NoContent() : Results.NotFound());
        }
    }
}
