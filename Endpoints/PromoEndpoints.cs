namespace JsonServer.Endpoints
{
    /// <summary>
    /// Publishing/backup/restore for the Canva-exported wwwroot/promozione.html.
    /// </summary>
    public static class PromoEndpoints
    {
        public static void MapPromoEndpoints(this IEndpointRouteBuilder app) =>
            app.MapPagePublisher("/api/promo", "promozione.html", "PromoBackups", "promozione");
    }
}
