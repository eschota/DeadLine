using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class WebServer
{
    /// <summary>
    /// Runs the web server asynchronously on the specified port, with CORS enabled to allow specific origins.
    /// </summary>
    /// <param name="port">The port on which the server will listen.</param>
    public static async Task RunServerAsync(int port)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = new string[] { },
            ApplicationName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
            ContentRootPath = Directory.GetCurrentDirectory(),
            WebRootPath = "wwwroot" // Designates the folder for static files
        });

        // Service configuration
        builder.Services.AddControllersWithViews(); // For using controllers and views
        builder.Services.AddCors(options => // Enable CORS
        {
            options.AddPolicy("AllowSpecificOrigin", builder =>
            {
                builder.WithOrigins("https://www.turbosquid.com") // Allow this origin
                       .AllowAnyHeader()
                       .AllowAnyMethod()
                       .AllowCredentials(); // Allow credentials for cross-origin requests
            });
        });

        var app = builder.Build();

        // Middleware to use CORS
        app.UseCors("AllowSpecificOrigin");

        // Route configuration
        app.MapGet("/", () => "Hello World!");

        app.MapPost("/api/data", async (HttpRequest request) =>
        {
            using var reader = new StreamReader(request.Body);
            var body = await reader.ReadToEndAsync();
            //body = body.Replace("procgartist", "MYFavoriteAuthor");
            // Process received data here...
            Console.WriteLine("Extension Update Page"); // Use Logger instead of Console.WriteLine
            body = body.Replace("3D", "5D");
            var response = Results.Content(body, "text/html; charset=utf-8");

            return response;
        });

        var provider = new FileExtensionContentTypeProvider();
        // Add MIME type for .crx files
        provider.Mappings[".crx"] = "application/x-chrome-extension";

        // Enable support for static files with customizable ContentTypeProvider
        app.UseStaticFiles(new StaticFileOptions
        {
            ContentTypeProvider = provider
        });

        app.Urls.Add($"http://*:{port}"); // Configure the server to listen on the specified port

        await app.RunAsync();
    }

    /// <summary>
    /// Adds a log message. Replace this with your actual logging mechanism.
    /// </summary>
    /// <param name="logMessage">The message to log.</param>
    public static void AddLog(string logMessage)
    {
        // Implementation of your logging mechanism. This is a placeholder.
        Console.WriteLine(logMessage);
    }
}