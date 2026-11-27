namespace JsonServer.Endpoints
{
    /// <summary>
    /// Publishing/backup/restore for wwwroot/assistenza.html (same mechanism as promo).
    /// </summary>
    public static class AssistenzaEndpoints
    {
        public static void MapAssistenzaEndpoints(this IEndpointRouteBuilder app) =>
            app.MapPagePublisher("/api/assistenza", "assistenza.html", "AssistenzaBackups", "assistenza");
    }
}
