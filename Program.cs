using Microsoft.EntityFrameworkCore;
using Vyuka.Models;
using Vyuka.Services;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;

var builder = WebApplication.CreateBuilder(args);

// Načtení SMTP nastavení
builder.Services.Configure<SmtpSettings>(
    builder.Configuration.GetSection("Smtp")
);

// Registrace EmailService
builder.Services.AddScoped<IEmailService, EmailService>();

// Registrace builderu e‑mailových šablon
builder.Services.AddScoped<LessonPlanEmailBuilder>();

// TemplateService
builder.Services.AddScoped<TemplateService>();

// 🔵 DŮLEŽITÉ – CHYBĚJÍCÍ REGISTRACE
builder.Services.AddScoped<LessonEmailBuilder>();

// Razor Pages + HttpContext + DB context
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Google Calendar API klient
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

// SESSION – nutné pro login a role
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(12);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

// 🔥 OCHRANA PŘED NEPŘIHLÁŠENÝMI UŽIVATELI (session varianta)
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower();

    // Stránky, které jsou veřejné
    var allowed = new[]
    {
        "/login",
        "/forgotpassword"
    };

    // Pokud není přihlášený a není na povolené stránce → redirect
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
app.MapGet("/", context =>
{
    context.Response.Redirect("/Dashboard/Admin");
    return Task.CompletedTask;
});


app.MapRazorPages();

app.Run();
