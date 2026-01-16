using Frontend_MarikinaAlert.Data;
using Frontend_MarikinaAlert.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Repository (Student 1)
builder.Services.AddScoped<IDisasterRepository, DisasterRepository>();

// 3. Bypass Service (TEMPORARY: Connects Frontend to DB while waiting for Student 2)
builder.Services.AddScoped<IDisasterTriageService, TemporaryTriageService>();

// ... existing code ...

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Reports}/{action=Create}/{id?}");

app.Run();
