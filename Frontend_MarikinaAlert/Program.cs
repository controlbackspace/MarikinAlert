using System.Globalization;
using Frontend_MarikinaAlert.Data;
using Frontend_MarikinaAlert.Localization;
using Frontend_MarikinaAlert.Repositories;
using Frontend_MarikinaAlert.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Resident pages can switch between English and Filipino; validation messages follow the same language.
builder.Services.AddSingleton<IStringLocalizerFactory, DictionaryStringLocalizerFactory>();
builder.Services.AddLocalization();
var mvc = builder.Services.AddControllersWithViews().AddDataAnnotationsLocalization();

// In Development, .cshtml edits show up on browser refresh without restarting the app.
if (builder.Environment.IsDevelopment()) mvc.AddRazorRuntimeCompilation();

// SQLite file lives next to the app so each node (admin laptop) has its own local copy.
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "marikinalert.db");
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? $"Data Source={dbPath};Default Timeout=30"));

builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddSingleton<IDisasterTriageService, KeywordTriageService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Login";
        options.LogoutPath = "/Admin/Logout";
        options.AccessDeniedPath = "/Admin/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(12); // one dispatcher shift
        options.SlidingExpiration = true;
        options.Cookie.Name = "MarikinAlert.Admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    SchemaPatches.Apply(db);
    // WAL lets the dashboard read while hundreds of residents are writing reports.
    db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/Error", "?code={0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

// Only the UI language switches; number and date formatting stays English so the dispatcher views don't change.
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en"),
    SupportedCultures = [new CultureInfo("en")],
    SupportedUICultures = [new CultureInfo("en"), new CultureInfo("fil")]
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Reports}/{action=Create}/{id?}");

app.Run();
