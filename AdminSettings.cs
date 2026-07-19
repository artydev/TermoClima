namespace JsonServer
{
    /// <summary>
    /// Bound from the "Admin" section of appsettings.json. Set the real
    /// password via `dotnet user-secrets` or an environment variable
    /// (Admin__Password) — never commit it to appsettings.json directly.
    /// </summary>
    public class AdminSettings
    {
        public string Password { get; set; } = "";
    }
}
