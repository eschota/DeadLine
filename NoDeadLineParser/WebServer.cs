using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using Telegram.Bot.Types;
using static System.Net.Mime.MediaTypeNames; 
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http; 
using System.Threading.Tasks;
using System.Runtime.Intrinsics.Arm;
using Telegram.Bot.Requests.Abstractions;
using Microsoft.AspNetCore.Http.HttpResults;
using static TSExtension;
using static System.Net.WebRequestMethods;

public static class WebServer
{
    public static async Task RunServerAsync(int port)
    {
        #region Ini
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

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowChromeExtensions", policy =>
            {
                policy.WithOrigins(
                        "chrome-extension://fciloakedckplbmonlkabbdheokjakmb",
                        "chrome-extension://fecldapeadcgpagoccpaimogilmopmhk",
                        "chrome-extension://bdjihjbaolhfkdjojhhdfffijcogflmm")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });

            options.AddPolicy("AllowSpecificOrigin", policy =>
            {
                policy.WithOrigins("https://www.turbosquid.com") // Specify the origin of your Chrome extension if it's hosted
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        // Настройка сервисов
        builder.Services.AddControllersWithViews();

        builder.Services.AddHttpClient();

        var app = builder.Build();

       // app.UseCors("AllowSpecificOrigin");

        app.UseCors("AllowChromeExtensions"); 

        app.MapGet("/", () => "Hello Nodes!");
        app.MapGet("/nodes", () => "Hello Я рендерфин!");
        app.MapGet("/api", () => "Hello Я suka!");
        #endregion


        #region ScriptsManager

        var scriptsManager = new ScriptsManager();

        app.MapGet("/get-scripts", async context =>
        {
            var client = context.RequestServices.GetRequiredService<HttpClient>();
            var result = new Dictionary<string, object>();

            // Define URLs to download and their corresponding keys
            var scriptUrls = new Dictionary<string, string>
                {
                    { "script_1", "https://renderfin.com/scriptsmanager/1.js" },
                    { "script_2", "https://renderfin.com/scriptsmanager/2.js" }
                };

            foreach (var (key, url) in scriptUrls)
            {
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var stream = await response.Content.ReadAsStreamAsync();
                    using (var sha256 = SHA256.Create())
                    {
                        var hash = BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                        result.Add(key, new { url, hash });
                    }
                }
                else
                {
                    // Handle error if download fails
                    result.Add(key, new { error = "Failed to download" });
                }
            }

            await context.Response.WriteAsJsonAsync(result);
        });
     
        


        app.MapGet("/api/scripts", (HttpContext context) => scriptsManager.GetScripts());
        app.MapPost("/api/scripts", async (HttpContext context) =>
        {
            var formFile = context.Request.Form.Files.FirstOrDefault();
            return formFile != null ? scriptsManager.UploadScript(formFile) : new BadRequestObjectResult("No file uploaded.");
        });
        app.MapDelete("/api/scripts/{fileName}", (string fileName) => scriptsManager.DeleteScript(fileName));

        #endregion
        #region TSParse        

        app.MapPost("/get-tasks", async (HttpRequest request) =>
        {
            using var reader = new StreamReader(request.Body);
            var json = await reader.ReadToEndAsync();
            try
            {
                iTask task = JsonConvert.DeserializeObject<iTask>(json);
                iWorker worker = BD.workers.Find(x => x.installId == task.installId);

                List<iTask> tasks = new List<iTask>();
                if (worker != null)
                {
                    tasks = worker.Update(task);

                    // Создаем список анонимных объектов, содержащих только installId
                    var tasksResponse = tasks.Select(t => new {
                        //t.installId,
                        taskId = t.taskId,
                        type = iTask.TaskType.PARSE,
                        payload = new
                        {
                            url = t.url,
                            delay = t.delay

                        },

                    }).ToList();
                    var jsonResponse = JsonConvert.SerializeObject(tasksResponse);
                    return Results.Content(jsonResponse, "application/json");
                }
                else
                {
                    return Results.BadRequest("No worker found");
                }
            }
            catch (Exception e)
            {
                return Results.BadRequest(e.Message);
            }
        });

        app.MapPost("/task-result", async (HttpRequest request) =>
        {
            using var reader = new StreamReader(request.Body);
            var json = await reader.ReadToEndAsync();
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(Program.TS.LocalPages[0].filePage))) Directory.CreateDirectory(Path.GetDirectoryName(Program.TS.LocalPages[0].filePage));

                iTask t = JsonConvert.DeserializeObject<iTask>(json);


                int lenght = Program.TS.Url.Length;

                iTask ft = BD.AllTasks.Find(X => X.taskId == t.taskId);
                int.TryParse(ft.url.Substring(lenght - 1), out lenght);

                string fPage = Path.Combine(Paths.ParseFolder, Program.TS.RawFolder, System.Text.RegularExpressions.Regex.Replace(DateTime.Now.Date.ToString("dd/MM/yyyy"), "[\\/:*?\"<>|]", "_"), lenght.ToString("0000") + ".html");

                if (t.taskResult.html.Length > 4000)
                {
                    System.IO.File.WriteAllText(fPage, t.taskResult.html);
                    Worker.Page ptoDelete = Program.TS.LocalPages.Find(x => x.url == ft.url);

                    Program.TS.LocalPages.Remove(ptoDelete);
                    Console.WriteLine("Page parsed: " + t.url + "  , " + Program.TS.LocalPages.Count);
                    TGBot.BotSendText(-4152887032, $"Pages To Parse: {Program.TS.LocalPages.Count}");
                }
                else
                {
                    TGBot.BotSendText(-4152887032, $"Captcha!!!: {t.nick}  {JsonConvert.SerializeObject(fPage)}");
                    return Results.BadRequest("Page not parsed, Captcha Possible!");                 
                }
                if ( t.runningTasks==null  || t.runningTasks.Length>0) return Results.Content("[]", "application/json");
                else
                {
                    iWorker worker = BD.workers.Find(x => x.installId == t.installId);

                    List<iTask> tasks = new List<iTask>();
                    if (worker != null)
                    {
                        tasks = worker.Update(t);

                        // Создаем список анонимных объектов, содержащих только installId
                        var tasksResponse = tasks.Select(t => new {
                            //t.installId,
                            taskId = t.taskId,
                            type = iTask.TaskType.PARSE,
                            payload = new
                            {
                                url = t.url,
                                delay = t.delay

                            },

                        }).ToList();
                        var jsonResponse = JsonConvert.SerializeObject(tasksResponse);
                        return Results.Content(jsonResponse, "application/json");
                    }
                }
            }
            catch (Exception ex) { return Results.BadRequest(ex.Message); }

           
            return Results.Content("Капун Кхап!", "text/html; charset=utf-8");
        });
        app.MapPost("/get-ts-trends", async (HttpRequest request) =>
        {
            try
            {
                using var reader = new StreamReader(request.Body);
                var json = await reader.ReadToEndAsync();
                if (string.IsNullOrWhiteSpace(json))
                {
                    return Results.BadRequest("Empty request body");
                }

                try
                {
                    var productContainer = JsonConvert.DeserializeObject<ProductContainer>(json);
                    if (productContainer?.Products == null || productContainer.Products.Length == 0)
                    {
                        return Results.BadRequest("No products provided or invalid JSON format");
                    }
                    // Вызов функции для получения данных по идентификаторам продуктов
                    string trends = TSExtension.GetTrendsByIds(productContainer.Products);
                    return Results.Content(trends, "application/json");
                }
                catch (JsonException je)
                {
                    return Results.BadRequest($"JSON Parsing Error: {je.Message}");
                }
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });


        app.MapPost("/signup", async (HttpRequest request) =>
        { 
            iTask t = new iWorker().SignUp();

       

        return Results.Content("{\"installId\": \"" + t.installId + "\"}", "application/json");
            
        });
        
        app.MapPost("/cgtrends", async (HttpRequest request) =>
        {
            Console.WriteLine("Extension try to get page to parse: ");
            return Results.Content(Program.TS.LocalPages[new Random().Next(0, Program.TS.LocalPages.Count)].url, "text/html; charset=utf-8");
        });
        #endregion
        #region Other
        app.MapPost("/", async (HttpRequest request) =>
        {
            using var reader = new StreamReader(request.Body);
            var json = await reader.ReadToEndAsync();

            Console.WriteLine("Node Received!");
            NodeStats.WriteJson(json); // Вызов функции обработчика с JSON в качестве аргумента
            
            return "Node Stats Received "; // Возвращаем результат функции обработчика в формате JSON
        });

        app.MapPost("/ingesters", async (HttpRequest request) =>
        {
            using var reader = new StreamReader(request.Body);
            var body = await reader.ReadToEndAsync();

            var splitIndex = body.IndexOf("<!--URL-->");
            if (splitIndex != -1)
            {
                var pageUrl = body.Substring(0, splitIndex);
                var htmlContent = body.Substring(splitIndex + "<!--URL-->".Length);

                Console.WriteLine($"Page URL: {pageUrl}");
                Injesters.ParseIngesters(htmlContent);
                
            }
            else
            {
                Console.WriteLine("Invalid data received");
            }

            return Results.Content("Data received", "text/plain");
        });   app.MapPost("/adminapprove", async (HttpRequest request) =>
        {
            using var reader = new StreamReader(request.Body);
            var body = await reader.ReadToEndAsync();

            var splitIndex = body.IndexOf("<!--URL-->");
            if (splitIndex != -1)
            {
                var pageUrl = body.Substring(0, splitIndex);
                var htmlContent = body.Substring(splitIndex + "<!--URL-->".Length);

                Console.WriteLine($"Page URL: {pageUrl}");
                Injesters.ParseAdminApprove(htmlContent);
                
            }
            else
            {
                Console.WriteLine("Invalid data received");
            }

            return Results.Content("Data received", "text/plain");
        });




        var provider = new FileExtensionContentTypeProvider();
        provider.Mappings[".crx"] = "application/x-chrome-extension";
        provider.Mappings[".br"] = "application/javascript";

        app.UseStaticFiles(new StaticFileOptions
        {
            ServeUnknownFileTypes = true, // Allow unknown file types
            DefaultContentType = "application/octet-stream", // Default MIME type for unrecognized files
            OnPrepareResponse = ctx =>
            {
                var path = ctx.File.PhysicalPath;
                if (path.EndsWith(".wasm.br"))
                {
                    ctx.Context.Response.Headers["Content-Encoding"] = "br"; // Specify Brotli encoding
                    ctx.Context.Response.ContentType = "application/wasm"; // Correct MIME type for WebAssembly files
                }
                else if (path.EndsWith(".br"))
                {
                    ctx.Context.Response.Headers["Content-Encoding"] = "br"; // Specify Brotli encoding
                                                                             // Use application/javascript or appropriate type based on the file being served
                    ctx.Context.Response.ContentType = "application/javascript";
                }
            }
        });


        //app.UseStaticFiles(new StaticFileOptions
        //{
        //    ContentTypeProvider = provider
        //});

        app.Urls.Add($"https://nodes.renderfin.com:{port}");
        app.Urls.Add($"https://cgtrends.renderfin.com:{port}");

        NodeStats.GenerateHTML(0);
        NodeStats.GenerateHTML(1);
        NodeStats.GenerateHTML(2);
        TGBot.BotSendText(-4152887032, "Server Started!");
        await app.RunAsync();
        Console.WriteLine("Server Started!");
        #endregion
    }
}