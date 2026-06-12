using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;
using Vyuka.Secrets;
using Vyuka.Services;

var builder = WebApplication.CreateBuilder(args);

// TimeProvider
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

// SMTP
builder.Services.Configure<SmtpSettings>(
    builder.Configuration.GetSection("SmtpSettings")
);

// Email services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<LessonPlanEmailBuilder>();
builder.Services.AddScoped<LessonEmailBuilder>();
builder.Services.AddScoped<OfferEmailBuilder>();

// QR kód
builder.Services.AddSingleton<QrCodeGeneratorService>();

// TemplateService
builder.Services.AddScoped<ITemplateService, TemplateService>();

// Razor Pages + DB
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Identity
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

// Cookie paths
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Login";
    options.AccessDeniedPath = "/AccessDenied";
});

// Authentication + Authorization
//builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

// Google Calendar
builder.Services.AddSingleton(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var env = provider.GetRequiredService<IHostEnvironment>();

    var keyPath = config["Google:ServiceAccountKeyPath"];
    keyPath = Path.Combine(env.ContentRootPath, keyPath);

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

builder.Services.AddScoped<GoogleCalendarService>();

// Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(12);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Role + Admin init
using (var scope = app.Services.CreateAsyncScope())
{
    var services = scope.ServiceProvider;

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<AppUser>>();

    string[] roleNames = { Roles.Admin, Roles.Teacher, Roles.Student };

    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    string adminEmail = "zakalois@ucitelzak.eu";
    string adminPassword = "zlastaLO";

    var admin = await userManager.FindByEmailAsync(adminEmail);

    if (admin == null)
    {
        admin = new AppUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FirstName = "Alois",
            LastName = "Žák",
            PhoneNumber = "601172322",
            PhotoPath = "",
            Role = Roles.Admin
        };

        var result = await userManager.CreateAsync(admin, adminPassword);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, Roles.Admin);
        }
        else
        {
            throw new Exception("Admin se nevytvořil: " +
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}

// Middleware pipeline
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

// Default redirect
app.MapGet("/", context =>
{
    context.Response.Redirect("/Login");
    return Task.CompletedTask;
});

app.MapRazorPages();

app.Run();
