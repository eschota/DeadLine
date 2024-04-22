using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class WebServer
{
    public static async Task RunServerAsync(int port)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = new string[] { },
            ApplicationName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
            ContentRootPath = Directory.GetCurrentDirectory(),
            WebRootPath = "wwwroot"
        });
        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.Listen(System.Net.IPAddress.Any, port, listenOptions =>
            {
                // Используем PFX файл для HTTPS
                listenOptions.UseHttps(@"c:\DeadLine\DeadLine\NoDeadLineParser\bin\Debug\net8.0\renderfin.com.pfx", "3dhelpmustdie");
            });
        });
        // Настройка сервисов
        builder.Services.AddControllersWithViews();

        var app = builder.Build();
        app.MapGet("/", () => "Hello Nodes!");
        app.MapGet("/nodes", () => "Hello Я рендерфин!");
        app.MapGet("/api", () => "Hello Я suka!");

        app.MapPost("/", async (HttpRequest request) =>
        {
            using var reader = new StreamReader(request.Body);
            var json = await reader.ReadToEndAsync();

            Console.WriteLine("Node Received!");
            NodeStats.WriteJson(json); // Вызов функции обработчика с JSON в качестве аргумента
            
            return "Node Stats Received "; // Возвращаем результат функции обработчика в формате JSON
        });
        var provider = new FileExtensionContentTypeProvider();
        provider.Mappings[".crx"] = "application/x-chrome-extension";
        app.UseStaticFiles(new StaticFileOptions
        {
            ContentTypeProvider = provider
        });

        app.Urls.Add($"https://nodes.renderfin.com:{port}");
        app.Urls.Add($"https://cgtrends.renderfin.com:{port}");

        NodeStats.GenerateHTML();
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

//app.MapPost("/tunnel-for-trends", async (HttpRequest request) =>
//{
//    Console.WriteLine("Tunnel Works!");
//    using var reader = new StreamReader(request.Body);
//    var body = await reader.ReadToEndAsync();

//    var splitIndex = body.IndexOf("<!--URL-->");
//    var pageUrl = body.Substring(0, splitIndex);
//    var htmlContent = body.Substring(splitIndex + "<!--URL-->".Length);

//    string newbody = TSExtension.ProcessKeyword(pageUrl, htmlContent) ?? TSExtension.TSProductSearch(htmlContent);

//    if (newbody == null)
//        return Results.Content("No content", "text/plain");

//    return Results.Content(newbody, "text/html; charset=utf-8");
//});
//app.Use(async (context, next) =>
//{
//    if (context.Request.Method == "OPTIONS")
//    {
//        var requestedOrigin = context.Request.Headers["Origin"].ToString();
//        if (requestedOrigin == "https://www.turbosquid.com" || requestedOrigin == "https://qwertystock.com")
//        {
//            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
//            context.Response.Headers.Add("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept");
//            context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
//            context.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
//            context.Response.StatusCode = 204; // No Content
//            return;
//        }
//        else
//        {
//            Console.WriteLine($"Invalid CORS attempt from origin: {requestedOrigin}");
//            context.Response.StatusCode = 403; // Forbidden
//            return;
//        }
//    }
//    await next();
//});

//Настройка маршрутов

//app.MapGet("/tunnel-for-trends", () => "Hello Tunnel!");

//app.MapPost("/nodes", async (HttpRequest request) =>
//{
//    Console.WriteLine("Node Received!");




//    return "Node Received!";
//});
