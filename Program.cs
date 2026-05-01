using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;
using Vyuka.Secrets;
using Vyuka.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------
// TimeProvider – nutné pro Identity v .NET 8
// ---------------------------------------------------------
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

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

// TemplateService
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
// Identity – čistá konfigurace bez migrací
// ---------------------------------------------------------
builder.Services.AddIdentityCore<IdentityUser>(options =>
{
    options.User.RequireUniqueEmail = true;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

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
// Inicializace rolí při startu aplikace (bez async/await)
// ---------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    string[] roleNames = { Roles.Admin, Roles.Teacher, Roles.Student };

    foreach (var roleName in roleNames)
    {
        var exists = roleManager.RoleExistsAsync(roleName).GetAwaiter().GetResult();
        if (!exists)
        {
            roleManager.CreateAsync(new IdentityRole(roleName)).GetAwaiter().GetResult();
        }
    }
}

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
