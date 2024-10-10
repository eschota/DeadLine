using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public static class ComfyUI_adapter
{
    private static readonly HttpClient client = new HttpClient();

    public static async Task<string> GenerateImage(string prompt)
    {
        string serverAddress = "http://5.129.157.224:8188";
        string clientId = "f47ac10b-58cc-4372-a567-0e02b2c3d479";

        // Формируем корректный JSON, заменяя $prompt на фактический текст
        string promptText = await File.ReadAllTextAsync("c:\\ComfyUI\\workflows\\workflow_flux.json");

        // Экранируем спецсимволы в prompt с помощью JsonSerializer
        string escapedPrompt = JsonSerializer.Serialize(prompt);

        // Убираем дополнительные кавычки, добавляемые JsonSerializer (оставляем только экранирование символов)
        escapedPrompt = escapedPrompt.Trim('"');

        // Вставляем экранированную строку в JSON
        promptText = promptText.Replace("$prompt", escapedPrompt);

        string outputImageName = DateTime.Now.ToFileTimeUtc().ToString();
        promptText = promptText.Replace("zzzz", outputImageName);

        // Отправляем запрос с правильной структурой JSON
        var response = await QueuePromptAsync(promptText, clientId, serverAddress);
        Console.WriteLine(response);

        // Проверяем статус генерации изображения
        string imageUrl = $"{serverAddress}/output/{outputImageName}_00001_.png";
        if (await IsImageAvailable(imageUrl))
        {
            Console.WriteLine($"Image generated and available at: {imageUrl}");

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
                await File.WriteAllBytesAsync(localPath, imageBytes);
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
            // Объединяем prompt и client_id в один объект JSON
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

            // Запускаем Python с скриптом main.py
            bool success = StartComfyUIServer();
            if (success)
            {
                // Ждем 10 секунд перед повторной попыткой
                await Task.Delay(10000);

                // Повторяем вызов функции
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
        int maxAttempts = 2440; // 240 попыток по 500 мс = 2 минуты
        int delay = 500; // Задержка между попытками в миллисекундах

        while (attempts < maxAttempts)
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(imageUrl);
                if (response.IsSuccessStatusCode)
                 
                {
                    var contentType = response.Content.Headers.ContentType.MediaType;
                    // Проверяем, что контент является изображением
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

            // Задержка перед следующей попыткой
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
}
