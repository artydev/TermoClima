using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using System.Xml.Linq;

namespace JsonServer
{
    public record InquiryItem(string Name, int Qty, decimal Price, string? Currency);

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
            var body = BuildOrderBody(order);
            var orderId = order.TryGetValue("id", out var idVal) ? idVal?.ToString() : "?";

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromAddress, _settings.FromName),
                Subject = $"Nuovo ordine – TermoClima (#{orderId})",
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

        private static string BuildOrderBody(Dictionary<string, object> order)
        {
            string Field(string key) => order.TryGetValue(key, out var v) ? v?.ToString() ?? "" : "";

            // deserialize to List<InquiryItem>
            var items = JsonSerializer.Deserialize<List<InquiryItem>>(Field("items")) as List<InquiryItem>;   //   Field("items") ;


            var lines = new List<string>
            {
                $"Nuovo ordine ricevuto — #{Field("id")}",
                "",
                $"Cliente: {Field("customerName")}",
                $"Email: {Field("email")}",
                $"Telefono: {Field("phone")}",
                $"Indirizzo: {Field("address")}, {Field("city")} {Field("postalCode")}".Trim().Trim(','),
                $"Ritiro/consegna: {Field("deliveryMethod")}",
                "",
                "Prodotti:",
            };

           
            
            foreach (var item in items)
            {
               
                lines.Add($"  {item.Qty}x {item.Name} ({item.Currency}{item.Price:0.00} cad.) — {item.Currency}{item.Price * item.Qty:0.00}");
            }
            

            lines.Add("");
            lines.Add($"Totale: €{Field("subtotal")}");

            var notes = Field("notes");
            if (!string.IsNullOrWhiteSpace(notes))
            {
                lines.Add("");
                lines.Add($"Note del cliente: {notes}");
            }

            lines.Add("");
            lines.Add("Il pagamento verrà concordato con il cliente (contanti, bonifico, POS in negozio o Klarna).");

            return string.Join("\n", lines);
        }
    }
}

