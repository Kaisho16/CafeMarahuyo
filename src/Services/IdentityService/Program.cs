using CafeMarahuyo.Shared.Auth;
using CafeMarahuyo.Shared.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<CafeDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add shared JWT Auth
builder.Services.AddSharedJwtAuthentication();

var app = builder.Build();

// Ensure DB is created and seeded (will use SQLite WAL for concurrency if we open it, but standard EF setup is fine)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CafeDbContext>();
    // Enable WAL mode
    context.Database.Migrate();
    // DbInitializer.Initialize is handled by InventoryService to prevent concurrent lock issues.
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Run on port 5101
app.Run("http://0.0.0.0:5101");
