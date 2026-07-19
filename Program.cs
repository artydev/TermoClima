using JsonServer;
using JsonServer.Data;
using JsonServer.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// --- Infrastructure & Services ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options => options.AddPolicy("Frontend", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var database = new Database("db.json");
builder.Services.AddSingleton(database);
builder.Services.AddHostedService<DatabaseAutoSaver>();

builder.Services.AddAuthentication("AdminCookie")
    .AddCookie("AdminCookie", opt => opt.LoginPath = "/login");
builder.Services.AddAuthorization();

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddSingleton<EmailService>();

builder.Services.Configure<AdminSettings>(builder.Configuration.GetSection("Admin"));

var app = builder.Build();

// --- Middleware Pipeline ---
app.UseAuthentication();
app.UseAuthorization();
app.UseCors("Frontend");
app.UseDefaultFiles();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (app.Environment.IsDevelopment())
        {
            // Actively changing constantly during dev — never let the browser cache
            // anything, so a normal reload always reflects the latest change. This
            // exact "still showing the old version" confusion has already happened
            // more than once without this.
            ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            ctx.Context.Response.Headers["Pragma"] = "no-cache";
            ctx.Context.Response.Headers["Expires"] = "0";
            return;
        }

        // Production: HTML pages always revalidate — cheap (they're small), and it
        // guarantees a page always references whatever CSS/JS is actually current.
        // CSS/JS/images get a real caching window so repeat visitors aren't
        // re-downloading them every visit; a day is short enough that a shipped
        // update still shows up promptly without needing manual cache-busting.
        if (ctx.File.Name.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Context.Response.Headers["Cache-Control"] = "no-cache, must-revalidate";
        }
        else
        {
            ctx.Context.Response.Headers["Cache-Control"] = "public, max-age=86400, must-revalidate";
        }
    }
});

app.UseStatusCodePagesWithReExecute("/404.html");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// --- Endpoints (see Endpoints/) ---
app.MapProductEndpoints(database, "orders");
app.MapOrderEndpoints();
app.MapAdminPageEndpoints();
app.MapAuthEndpoints();
app.MapMediaEndpoints();
app.MapInquiryEndpoints();
app.MapPromoEndpoints();

app.Run();
