namespace JsonServer
{
    /// <summary>
    /// Bound from the "Smtp" section of appsettings.json. Real credentials
    /// (User/Password/FromAddress) should never be committed to source
    /// control — set them via `dotnet user-secrets` for local development,
    /// or environment variables (Smtp__User, Smtp__Password, etc.) in
    /// whatever hosting environment this actually runs in.
    /// </summary>
    public class SmtpSettings
    {
        public string Host { get; set; } = "smtp.laposte.net";
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string User { get; set; } = "";
        public string Password { get; set; } = "";
        public string FromAddress { get; set; } = "";
        public string FromName { get; set; } = "TermoClima";
        public string ToAddress { get; set; } = "salve.didio@laposte.net";

        public string BaseUrl { get; set; } = "https://termoclima.runasp.net/";

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(User) &&
            !string.IsNullOrWhiteSpace(Password) &&
            !string.IsNullOrWhiteSpace(FromAddress);
    }
}
