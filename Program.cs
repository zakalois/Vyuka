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
// Identity – správná konfigurace pro AppUser
// ---------------------------------------------------------
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;

    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// ---------------------------------------------------------
// Authentication + Authorization
// ---------------------------------------------------------
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

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
// Inicializace rolí + ADMIN účtu při startu aplikace
// ---------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

    string[] roleNames = { Roles.Admin, Roles.Teacher, Roles.Student };

    foreach (var roleName in roleNames)
    {
        var exists = roleManager.RoleExistsAsync(roleName).GetAwaiter().GetResult();
        if (!exists)
        {
            roleManager.CreateAsync(new IdentityRole(roleName)).GetAwaiter().GetResult();
        }
    }

    // ---------------------------------------------------------
    // AUTOMATICKÉ VYTVOŘENÍ ADMINA, POKUD NEEXISTUJE
    // ---------------------------------------------------------
    string adminEmail = "zakalois@ucitelzak.eu";
    string adminPassword = "zlastaLO";

    var admin = userManager.FindByEmailAsync(adminEmail).GetAwaiter().GetResult();

    if (admin == null)
    {
        admin = new AppUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,

            // ✔ doplněné údaje
            FirstName = "Alois",
            LastName = "Žák",
            PhoneNumber = "",
            PhotoPath = "",
            Role = Roles.Admin
        };

        var result = userManager.CreateAsync(admin, adminPassword).GetAwaiter().GetResult();

        if (result.Succeeded)
        {
            userManager.AddToRoleAsync(admin, Roles.Admin).GetAwaiter().GetResult();
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
app.UseAuthentication();
app.UseAuthorization();

// ---------------------------------------------------------
// Ochrana před nepřihlášenými uživateli
// ---------------------------------------------------------
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower();

    var allowed = new[]
    {
        "/login",
        "/forgotpassword",
        "/account/resetpassword"
    };

    if (!allowed.Contains(path))
    {
        var userId = context.Session.GetString("UserId");
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
    context.Response.Redirect("/Admin/Dashboard");
    return Task.CompletedTask;
});

app.MapRazorPages();

app.Run();
