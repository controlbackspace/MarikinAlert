using Frontend_MarikinaAlert.Data;
using Frontend_MarikinaAlert.Repositories;
using Frontend_MarikinaAlert.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var mvc = builder.Services.AddControllersWithViews();

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

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Reports}/{action=Create}/{id?}");

app.Run();
