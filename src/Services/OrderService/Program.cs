using CafeMarahuyo.Shared.Auth;
using CafeMarahuyo.Shared.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add DbContext (Wait, we should configure it via code or appsettings? We'll use the hardcoded path or standard connection string)
// The database is stored at "data/cafe_marahuyo.db" relative to root, but it's executed from root.
// Let's use the configuration.
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

var connectionString = builder.Configuration.GetConnectionString("PosConnection") 
    ?? "Data Source=../../../data/cafe_pos.db";

builder.Services.AddDbContext<PosDbContext>(options =>
    options.UseNpgsql(connectionString));

var cafeConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<CafeDbContext>(options =>
    options.UseNpgsql(cafeConnectionString));

// Add shared JWT Auth
builder.Services.AddSharedJwtAuthentication();

var app = builder.Build();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var posContext = scope.ServiceProvider.GetRequiredService<PosDbContext>();
    PosDbInitializer.Initialize(posContext);
}

var portStr = Environment.GetEnvironmentVariable("PORT") ?? "5105";
app.Run($"http://0.0.0.0:{portStr}");
