namespace JsonServer.Endpoints
{
    public static class InquiryEndpoints
    {
        public static void MapInquiryEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("/api/inquiry", async (InquiryRequest request, EmailService email) =>
            {
                if (request.Items is null || request.Items.Count == 0)
                    return Results.BadRequest(new { error = "Il carrello è vuoto." });

                if (!email.IsConfigured)
                    return Results.Problem(
                        "SMTP non configurato sul server: imposta Smtp:User, Smtp:Password e Smtp:FromAddress.",
                        statusCode: StatusCodes.Status500InternalServerError);

                try
                {
                    await email.SendInquiryAsync(request);
                    return Results.Ok(new { sent = true });
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Invio email fallito: {ex.Message}", statusCode: StatusCodes.Status502BadGateway);
                }
            });
        }
    }
}
