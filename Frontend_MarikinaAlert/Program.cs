using Frontend_MarikinaAlert.Data;
using Frontend_MarikinaAlert.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Repository
builder.Services.AddScoped<IDisasterRepository, DisasterRepository>();

// 3. Service Registration
builder.Services.AddScoped<IDisasterTriageService, TemporaryTriageService>();

// 4. ENABLE SESSION & HTTP ACCESSOR
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// <--- ADD THIS NEW LINE HERE! --->
builder.Services.AddHttpContextAccessor();
// <--------------------------------->

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 5. ACTIVATE SESSION
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Reports}/{action=Index}/{id?}"); // Default to Feed

app.Run();