using CafeMarahuyo.Shared.Auth;
using CafeMarahuyo.Shared.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add DbContexts
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
    
builder.Services.AddDbContext<CafeDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddDbContext<PosDbContext>(options =>
    options.UseNpgsql(connectionString)); // Use same connection string for now

// Add shared JWT Auth
builder.Services.AddSharedJwtAuthentication();

// Enable CORS if needed (for mobile/external apps in the future)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader());
});

// Register Domain Services (OOP Pattern)
builder.Services.AddScoped<CafeMarahuyo.Api.Services.IInventoryManager, CafeMarahuyo.Api.Services.InventoryManager>();

var app = builder.Build();

// Ensure DB is created and seeded on startup
using (var scope = app.Services.CreateScope())
{
    var cafeContext = scope.ServiceProvider.GetRequiredService<CafeDbContext>();
    var posContext = scope.ServiceProvider.GetRequiredService<PosDbContext>();
    
    // Migrate and seed Data
    DbInitializer.Initialize(cafeContext);
    PosDbInitializer.Initialize(posContext);
}

app.UseCors("AllowAll");

// Serve static files from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map fallback to index.html for SPA if necessary (but dashboard.html is separate)
// app.MapFallbackToFile("index.html");

var portStr = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0.0:{portStr}");
