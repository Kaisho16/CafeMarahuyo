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

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CafeDbContext>();
    context.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
    // DbInitializer.Initialize is handled by InventoryService to prevent concurrent lock issues.
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Run on port 5103
app.Run("http://localhost:5103");
