using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using System.Globalization;

namespace JsonServer
{
    public record InquiryItem(string ProductId, string Name, int Qty, decimal Price, string? Currency);

    public record InquiryRequest(List<InquiryItem> Items, decimal Subtotal);

    public record OrderRequest(
        string CustomerName,
        string Email,
        string? Phone,
        string? Address,
        string? City,
        string? PostalCode,
        string? DeliveryMethod,
        string? Notes,
        List<InquiryItem> Items,
        decimal Subtotal
    );

    public class EmailService
    {
        private readonly SmtpSettings _settings;

        public EmailService(IOptions<SmtpSettings> settings)
        {
            _settings = settings.Value;
        }

        public bool IsConfigured => _settings.IsConfigured;

        public async Task SendInquiryAsync(InquiryRequest request)
        {
            var body = BuildInquiryBody(request);

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromAddress, _settings.FromName),
                Subject = "Richiesta informazioni – TermoClima",
                Body = body,
                IsBodyHtml = false,
            };
            message.To.Add(_settings.ToAddress);

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.User, _settings.Password),
            };

            await client.SendMailAsync(message);
        }

        // `order` is whatever Database.Insert() handed back — a schema-less
        // Dictionary<string, object>, same shape as a saved product. Values
        // come back as JsonElement (System.Text.Json's default for an
        // object-typed dictionary), so we read them defensively rather than
        // assuming a fixed .NET type.
        public async Task SendOrderConfirmationAsync(Dictionary<string, object> order)
        {
            var body = BuildOrderBody(order, _settings.BaseUrl);
            var orderId = order.TryGetValue("id", out var idVal) ? idVal?.ToString() : "?";

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromAddress, _settings.FromName),
                Subject = $"Nuovo ordine – TermoClima (#{orderId}",
                Body = body,
                IsBodyHtml = true,
            };
            message.To.Add(_settings.ToAddress);

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.User, _settings.Password),
            };

            await client.SendMailAsync(message);
        }

        // Mirrors the exact wording the old mailto: link used to build client-side,
        // so the email a shopkeeper receives reads the same as before.
        private static string BuildInquiryBody(InquiryRequest request)
        {
            static string FormatPrice(decimal price, string? currency) =>
                $"{currency ?? "€"}{price:0.00}";

            var lines = request.Items.Select(item =>
                $"{item.Qty}x {item.Name} ({FormatPrice(item.Price, item.Currency)} cad.) — {FormatPrice(item.Price * item.Qty, item.Currency)}"
            );

            var parts = new List<string>
            {
                "Salve,",
                "",
                "vorrei ricevere informazioni sulla disponibilità e sui tempi di consegna dei seguenti prodotti:",
                "",
            };
            parts.AddRange(lines);
            parts.Add("");
            parts.Add($"Totale: {FormatPrice(request.Subtotal, "€")}");
            parts.Add("");
            parts.Add("Resto in attesa di un vostro riscontro.");
            parts.Add("Grazie,");

            return string.Join("\n", parts);
        }


        // Build Order to be sent via SMTP with  HTML formatti
        private static string BuildOrderBody(Dictionary<string, object> order, string baseUrl)
        {
            var it = new CultureInfo("it-IT");
            
            string Field(string key) => order.TryGetValue(key, out var v) ? v?.ToString() ?? "" : "";

            List<InquiryItem> items = new();
            try { items = JsonSerializer.Deserialize<List<InquiryItem>>(Field("items")) ?? new(); }
            catch { }

            string email = Field("email");

            string itemsHtml = string.Join("\n", items.Select((item, index) =>
                $"""
                    <tr style="background-color: {(index % 2 == 0 ? "#ffffff" : "#f9f9f9")};">
                        <td style="padding: 12px; border-bottom: 1px solid #eee;">
                            <a href="{baseUrl}/prodotto.html?id={item.ProductId}">
                               {item.Name}
                            </a>
                        </td>
                     
                        <td style="padding: 12px; border-bottom: 1px solid #eee; color: #555; text-align: center;">{item.Qty}</td>
                        <td style="padding: 12px; border-bottom: 1px solid #eee; color: #555; text-align: right;">{(item.Price * item.Qty).ToString("N2", it)} {item.Currency}</td>
                    </tr>
                """
            ));

            // 3. Conditional sections
            string notes = Field("notes");
                string notesHtml = string.IsNullOrWhiteSpace(notes) ? "" :
                    $"""
                <div style="background-color: #fff3cd; border-left: 4px solid #ffecb5; padding: 15px; margin-top: 20px;">
                    <strong style="color: #856404;">Note del cliente:</strong>
                    <p style="margin: 5px 0 0 0; color: #856404;">{notes}</p>
                </div>
                """;

                // 4. Calculate/Format Subtotal
                // Attempt to parse the string to decimal, otherwise just display the raw value
                decimal.TryParse(Field("subtotal"), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal subtotal);
                string formattedSubtotal = subtotal.ToString("N2", it);

                // 5. Return Final HTML Template
                return $"""
            <div style="font-family: 'Segoe UI', Arial, sans-serif; background-color: #f4f4f4; padding: 40px 20px;">
                <table width="600" align="center" style="background-color: #ffffff; border-radius: 8px; overflow: hidden; border: 1px solid #e0e0e0; box-shadow: 0 4px 6px rgba(0,0,0,0.1);">
                    <tr>
                        <td style="background-color: #005600; padding: 25px; text-align: center; ">
                            <h1 style="margin: 0; font-size: 24px; color:white;">TermoClima</h1>
                        </td>
                    </tr>
                    <tr>
                    
                    </tr>
                    <tr>
                        <td style="padding: 30px;">
                            <h2 style="color: #2c3e50; margin-top: 0;">Nuovo Ordine</h2>
                            <h2  style="color: #2c3e50;">Client Email : {email}</h2>
                            <table width="100%" style="margin-top: 30px; border-collapse: collapse; font-size: 14px;">
                                <thead>
                                    <tr style="background-color: #ecf0f1;">
                                        <th style="padding: 12px; text-align: left; color: #2c3e50;">Prodotto</th>
                                       
                                        <th style="padding: 12px; text-align: center; color: #2c3e50;">Qta</th>
                                        <th style="padding: 12px; text-align: right; color: #2c3e50;">Totale</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {itemsHtml}
                                </tbody>
                            </table>
                            <div style="text-align: right; margin-top: 20px;">
                                <span style="font-size: 18px; color: #2c3e50;"><strong>Totale: {formattedSubtotal} €</strong></span>
                            </div>
                            {notesHtml}
                        </td>
                    </tr>
                </table>
            </div>
            """;
            }

        }
}

