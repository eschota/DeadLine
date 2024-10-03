using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public static class ComfyUI_adapter
{
    private static readonly HttpClient client = new HttpClient();

    public static async Task TestRun(string prompt)
    {
        string serverAddress = "http://127.0.0.1:8188";
        string clientId = "f47ac10b-58cc-4372-a567-0e02b2c3d479";

        // Формируем корректный JSON, заменяя $prompt на фактический текст
        string promptText = await File.ReadAllTextAsync("c:\\ComfyUI\\workflows\\workflow_sdxl_prompt.json");

        // Отправляем запрос с правильной структурой JSON
        var response = await QueuePromptAsync(promptText, clientId, serverAddress);
        Console.WriteLine(response);
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
