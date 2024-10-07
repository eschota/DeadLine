using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public static class ComfyUI_adapter
{
    private static readonly HttpClient client = new HttpClient();

    public static async Task<string> GenerateImage(string prompt, string filePath)
    {
        string serverAddress = "http://127.0.0.1:8188";
        string clientId = "f47ac10b-58cc-4372-a567-0e02b2c3d479";

        // Формируем корректный JSON, заменяя $prompt на фактический текст
        string promptText = await File.ReadAllTextAsync("c:\\ComfyUI\\workflows\\workflow_sdxl_prompt.json");
        promptText = promptText.Replace("$prompt", prompt);

        string outputImageName = DateTime.Now.ToFileTimeUtc().ToString();
        promptText = promptText.Replace("zzzz", outputImageName);

        // Отправляем запрос с правильной структурой JSON
        var response = await QueuePromptAsync(promptText, clientId, serverAddress);
        Console.WriteLine(response);

        string outputPath = $"c:\\ComfyUI\\output\\{outputImageName}_00001_.png";

        // Ждем, пока файл не будет доступен или истечет время ожидания (10 секунд)
        int attempts = 0;
        int maxAttempts = 100; // 100 попыток с задержкой в 100 мс (в сумме 10 секунд)

        while (attempts < maxAttempts)
        {
            try
            {
                // Проверяем, доступен ли файл для чтения
                using (FileStream stream = new FileStream(outputPath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    // Если удалось открыть файл для чтения, значит он готов
                    Console.WriteLine($"Image generated and saved at: {outputPath}");
                    return outputPath;
                }
            }
            catch (IOException)
            {
                // Файл пока занят другим процессом, ждем 100 миллисекунд и пробуем снова
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                return null; // Или вернуть сообщение об ошибке, если нужно
            }

            attempts++;
        }

        Console.WriteLine("Error: File generation timed out.");
        return null; // Если файл так и не стал доступным
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
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}
