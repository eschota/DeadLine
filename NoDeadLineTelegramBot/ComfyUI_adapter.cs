using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Collections.Generic;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
public class Server
{
    public string Address { get; set; }
    public string Name { get; set; }
    public string GPU { get; set; }
    public int CurrentQueueCount { get; private set; } // Размер текущей очереди

    private static readonly HttpClient client = new HttpClient();

    public Server(string address, string name, string gpu)
    {
        Address = address;
        Name = name;
        GPU = gpu;
        CurrentQueueCount = -1; // Инициализируем как -1, если данные еще не получены
    }

    // Метод для обновления размера очереди
    public async Task UpdateQueueSizeAsync()
    {
        try
        {
            HttpResponseMessage response = await client.GetAsync($"{Address}/queue");

            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                using JsonDocument document = JsonDocument.Parse(jsonResponse);

                JsonElement root = document.RootElement;

                int runningQueueSize = root.GetProperty("queue_running").GetArrayLength();
                int pendingQueueSize = root.GetProperty("queue_pending").GetArrayLength();

                // Обновляем текущее количество задач в очереди
                CurrentQueueCount = runningQueueSize + pendingQueueSize;
            }
            else
            {
                Console.WriteLine($"Ошибка при получении данных очереди с сервера {Name}: {response.StatusCode}");
                CurrentQueueCount = -1; // Ошибка при запросе
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при обновлении очереди на сервере {Name}: {ex.Message}");
            CurrentQueueCount = -1; // Ошибка при запросе
        }
    }
    public async Task<int> GetQueueSize()
    {
        try
        {
            HttpResponseMessage response = await client.GetAsync($"{Address}/queue");

            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                using JsonDocument document = JsonDocument.Parse(jsonResponse);

                JsonElement root = document.RootElement;

                // Извлекаем количество задач в очереди, которые выполняются (queue_running)
                int runningQueueSize = root.GetProperty("queue_running").GetArrayLength();

                // Извлекаем количество задач, ожидающих выполнения (queue_pending)
                int pendingQueueSize = root.GetProperty("queue_pending").GetArrayLength();

                // Возвращаем общую длину очереди
                return runningQueueSize + pendingQueueSize;
            }
            else
            {
                Console.WriteLine($"Error fetching queue size for server {Name}: {response.StatusCode}");
                return -1; // Ошибка запроса
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking queue size for server {Name}: {ex.Message}");
            return -1; // Ошибка
        }
    }

    // Метод для проверки доступности сервера
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            HttpResponseMessage response = await client.GetAsync($"{Address}/queue");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

public static class ComfyUI_adapter
{
    private static readonly HttpClient client = new HttpClient();
    public static readonly List<Server> AvailableServers = new List<Server>
    {
        new Server("http://5.129.157.224:8188", "Vlad 3080rtx", "3080rtx"),
        new Server("http://5.129.157.224:8288", "Raptor 3070ti", "3070ti"),
        new Server("http://37.192.2.126:8188", "Nount 3070ti", "3070ti")
    };

    public static async Task<Server> GetLeastLoadedServer()
    {
        Server leastLoadedServer = null;
        int minQueueSize = int.MaxValue;

        foreach (var server in AvailableServers)
        {
            await server.UpdateQueueSizeAsync(); // Обновляем очередь для каждого сервера
            if (server.CurrentQueueCount >= 0 && server.CurrentQueueCount < minQueueSize)
            {
                minQueueSize = server.CurrentQueueCount;
                leastLoadedServer = server;
            }
        }

        return leastLoadedServer;
    }

    public static async Task<string> GenerateImage(string prompt, Message message, string serverAddress = "http://5.129.157.224:8188")
    { 
        string clientId = "f47ac10b-58cc-4372-a567-0e02b2c3d479";

        // Формируем корректный JSON, заменяя $prompt на фактический текст
        if (!System.IO.File.Exists("c:\\ComfyUI\\workflows\\workflow_flux.json"))
        {
            Console.WriteLine("Error: workflow file does not exist.");
            return null;
        }

        string promptText = await System.IO.File.ReadAllTextAsync("c:\\ComfyUI\\workflows\\workflow_flux.json");
        if (string.IsNullOrEmpty(promptText))
        {
            Console.WriteLine("Error: workflow file is empty.");
            return null;
        }

        // Экранируем спецсимволы в prompt с помощью JsonSerializer
        string escapedPrompt = JsonSerializer.Serialize(prompt).Trim('"');

        // Вставляем экранированную строку в JSON
        promptText = promptText.Replace("$prompt", escapedPrompt);

        string outputImageName = DateTime.Now.ToFileTimeUtc().ToString();
        promptText = promptText.Replace("zzzz", outputImageName);

        // Отправляем запрос с правильной структурой JSON
        var response = await QueuePromptAsync(promptText, clientId, serverAddress);
        Console.WriteLine(response);

        // Проверяем статус генерации изображения
        string imageUrl = $"{serverAddress}/output/{outputImageName}_00001_.png";
        bool imageAvailable = await IsImageAvailable(imageUrl);

        if (!imageAvailable)
        {
            Console.WriteLine("Image was not available within the timeout period, retrying...");

            // Повторная попытка генерации изображения
            response = await QueuePromptAsync(promptText, clientId, serverAddress);
            Console.WriteLine(response);

            // Проверяем статус еще раз
            imageAvailable = await IsImageAvailable(imageUrl);
        }

        if (imageAvailable)
        {
            Console.WriteLine($"Image generated and available at: {imageUrl}");



            Chat.SendToApi(message, imageUrl);
            // Получаем имя файла из URL
            string fileName = Path.GetFileName(new Uri(imageUrl).AbsolutePath);
            string localImagePath = Path.Combine("c:\\comfyui\\output\\", fileName);

            // Скачиваем изображение в папку c:\comfyui\output\ с тем же именем
            string downloadedPath = await DownloadImageLocally(imageUrl, localImagePath);

            if (downloadedPath != null)
            {
                Console.WriteLine($"Image downloaded and saved at: {downloadedPath}");
                return downloadedPath;
            }
            else
            {
                Console.WriteLine("Failed to download the image locally.");
                return null;
            }
        }

        Console.WriteLine("Error: Image generation failed or image is not available.");
        return null;
    }

    private static async Task<string> DownloadImageLocally(string imageUrl, string localPath)
    {
        try
        {
            HttpResponseMessage response = await client.GetAsync(imageUrl);
            if (response.IsSuccessStatusCode)
            {
                byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
                await System.IO.File.WriteAllBytesAsync(localPath, imageBytes);
                return localPath;
            }
            else
            {
                Console.WriteLine($"Error downloading image: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading image: {ex.Message}");
        }
        return null;
    }

    public static async Task<string> QueuePromptAsync(string prompt, string clientId, string serverAddress)
    {
        try
        {
            var payload = new
            {
                prompt = JsonSerializer.Deserialize<JsonElement>(prompt),
                client_id = clientId
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync($"{serverAddress}/prompt", content);

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                TrackProgress(prompt, clientId);
                return responseBody;
            }
            else
            {
                return $"Error: {response.StatusCode}";
            }
        }
        catch (HttpRequestException)
        {
            Console.WriteLine("Server is not responding. Restarting ComfyUI...");

            bool success = StartComfyUIServer();
            if (success)
            {
                await Task.Delay(10000);
                return await QueuePromptAsync(prompt, clientId, serverAddress);
            }
            else
            {
                return "Error: Failed to start ComfyUI server.";
            }
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    private static async Task<bool> IsImageAvailable(string imageUrl)
    {
        int attempts = 0;
        int maxAttempts = 240; // 240 попыток по 500 мс = 2 минуты
        int delay = 500; // Задержка между попытками в миллисекундах

        while (attempts < maxAttempts)
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(imageUrl);
                if (response.IsSuccessStatusCode)
                {
                    var contentType = response.Content.Headers.ContentType.MediaType;
                    if (contentType.StartsWith("image/"))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking image availability: {ex.Message}");
            }

            await Task.Delay(delay);
            attempts++;
        }

        Console.WriteLine("Image did not become available within the timeout period.");
        return false;
    }

    private static bool StartComfyUIServer()
    {
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = "c:/comfyui/main.py",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process process = new Process
            {
                StartInfo = startInfo
            };

            process.Start();

            Console.WriteLine("Waiting for ComfyUI server to start...");

            int retries = 0;
            int maxRetries = 10;
            int delay = 1000; // 1 секунда между проверками

            while (retries < maxRetries)
            {
                if (IsServerAvailable("http://127.0.0.1:8188"))
                {
                    Console.WriteLine("ComfyUI server started successfully.");
                    return true;
                }

                retries++;
                Task.Delay(delay).Wait();
            }

            Console.WriteLine("Failed to verify ComfyUI server startup within the timeout period.");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting ComfyUI server: {ex.Message}");
            return false;
        }
    }

    private static bool IsServerAvailable(string serverAddress)
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = client.GetAsync(serverAddress).Result;
                return response.IsSuccessStatusCode;
            }
        }
        catch
        {
            return false;
        }
    }

    public static void TrackProgress(string prompt, string clientId)
    {
        // Эмуляция подключения WebSocket для отслеживания прогресса
        Console.WriteLine("Tracking progress for the prompt...");
        // Здесь можно добавить реализацию WebSocket для отслеживания прогресса,
        // где мы будем получать информацию о прогрессе выполнения и обновлять статус.
    }
}
