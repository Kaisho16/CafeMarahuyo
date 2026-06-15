using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Dynamically patch YARP config with Render hostports if available
string[] services = { "auth", "inventory", "transactions", "orders" };
foreach (var s in services) {
    var hostEnv = Environment.GetEnvironmentVariable($"{s.ToUpper()}_HOST");
    if (!string.IsNullOrEmpty(hostEnv)) {
        // Revert back to using the exact string Render provides (hostname:port)
        builder.Configuration[$"ReverseProxy:Clusters:{s}-cluster:Destinations:destination1:Address"] = $"http://{hostEnv}";
    }
}

// Add YARP
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapGet("/api/ping", async () => {
    var authHost = Environment.GetEnvironmentVariable("AUTH_HOST");
    var results = new System.Text.StringBuilder();
    using var client = new System.Net.Http.HttpClient();
    client.Timeout = TimeSpan.FromSeconds(5);
    
    try {
        var res = await client.GetAsync($"http://{authHost}/api/auth/me");
        results.AppendLine($"Ping http://{authHost}: {res.StatusCode}");
    } catch (Exception ex) {
        results.AppendLine($"Ping http://{authHost} FAILED: {ex.Message}");
    }
    
    var hostOnly = authHost?.Split(':')[0];
    try {
        var res2 = await client.GetAsync($"http://{hostOnly}:10000/api/auth/me");
        results.AppendLine($"Ping http://{hostOnly}:10000: {res2.StatusCode}");
    } catch (Exception ex) {
        results.AppendLine($"Ping http://{hostOnly}:10000 FAILED: {ex.Message}");
    }

    return results.ToString();
});

// Serve static files from wwwroot
app.UseStaticFiles();

// Setup routing
app.UseRouting();

// Map YARP routes
app.MapReverseProxy();

// Fallback for SPA routing - serve index.html for unknown routes 
// that are not API calls or static files.
app.MapFallback(async context =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteAsJsonAsync(new { error = "Endpoint not found" });
        return;
    }

    var htmlPath = Path.Combine(app.Environment.WebRootPath, "index.html");
    if (File.Exists(htmlPath))
    {
        context.Response.ContentType = "text/html";
        await context.Response.SendFileAsync(htmlPath);
    }
    else
    {
        context.Response.StatusCode = 404;
    }
});

app.MapGet("/api/debug", () => new
{
    authHost = Environment.GetEnvironmentVariable("AUTH_HOST"),
    inventoryHost = Environment.GetEnvironmentVariable("INVENTORY_HOST"),
    ordersHost = Environment.GetEnvironmentVariable("ORDERS_HOST"),
    transactionsHost = Environment.GetEnvironmentVariable("TRANSACTIONS_HOST"),
    port = Environment.GetEnvironmentVariable("PORT")
});

// Run the application on port 5000
var portStr = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Run($"http://0.0.0.0:{portStr}");
