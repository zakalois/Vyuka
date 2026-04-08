using Microsoft.EntityFrameworkCore;
using Vyuka.Models;
using Vyuka.Services;

var builder = WebApplication.CreateBuilder(args);

// Načtení SMTP nastavení
builder.Services.Configure<SmtpSettings>(
    builder.Configuration.GetSection("Smtp")
);

// Registrace EmailService
builder.Services.AddScoped<IEmailService, EmailService>();

// Registrace TemplateService (DŮLEŽITÉ!)
builder.Services.AddScoped<ITemplateService, TemplateService>();

// Registrace Razor Pages, DB contextu atd.
builder.Services.AddRazorPages();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();