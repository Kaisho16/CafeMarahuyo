using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Add YARP
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

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

// Run the application on port 5000
app.Run("http://0.0.0.0:5000");
