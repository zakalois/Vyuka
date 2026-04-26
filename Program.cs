using Microsoft.EntityFrameworkCore;
using Vyuka.Models;
using Vyuka.Services;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------
// SMTP nastavení
// ---------------------------------------------------------
builder.Services.Configure<SmtpSettings>(
    builder.Configuration.GetSection("SmtpSettings")
);

// EmailService
builder.Services.AddScoped<IEmailService, EmailService>();

// ---------------------------------------------------------
// Šablony e‑mailů
// ---------------------------------------------------------
builder.Services.AddScoped<LessonPlanEmailBuilder>();
builder.Services.AddScoped<LessonEmailBuilder>();
builder.Services.AddScoped<OfferEmailBuilder>();

// TemplateService (správně přes interface)
builder.Services.AddScoped<ITemplateService, TemplateService>();

// ---------------------------------------------------------
// Razor Pages + DB + HttpContext
// ---------------------------------------------------------
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// ---------------------------------------------------------
// Google Calendar API klient
// ---------------------------------------------------------
builder.Services.AddSingleton(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var keyPath = config["Google:ServiceAccountKeyPath"];

    keyPath = Path.GetFullPath(keyPath);

    if (!File.Exists(keyPath))
        throw new FileNotFoundException($"Service account JSON nenalezen: {keyPath}");

    var credential = GoogleCredential
        .FromFile(keyPath)
        .CreateScoped(CalendarService.Scope.Calendar)
        .CreateWithUser("zakalois@ucitelzak.eu");

    return new CalendarService(
        new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "VyukaApp"
        });
});

// Služba pro práci s Google Calendar
builder.Services.AddScoped<GoogleCalendarService>();

// ---------------------------------------------------------
// SESSION
// ---------------------------------------------------------
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(12);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// ---------------------------------------------------------
// Pipeline
// ---------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

// ---------------------------------------------------------
// Ochrana před nepřihlášenými uživateli
// ---------------------------------------------------------
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower();

    var allowed = new[]
    {
        "/login",
        "/forgotpassword"
    };

    if (!allowed.Contains(path))
    {
        var userId = context.Session.GetInt32("UserId");
        if (userId == null)
        {
            context.Response.Redirect("/Login");
            return;
        }
    }

    await next();
});

// Defaultní redirect
app.MapGet("/", context =>
{
    context.Response.Redirect("/Dashboard/Admin");
    return Task.CompletedTask;
});

app.MapRazorPages();

app.Run();
