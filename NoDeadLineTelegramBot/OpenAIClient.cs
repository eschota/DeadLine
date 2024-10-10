using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;

public static class OpenAIClient
    {
    public static async Task<string> AskOpenAI(string _prompt, string _model="")
    { 
        if(_model=="")
        { // _model = "o1-preview",
            _model = "gpt-4o-mini";
        }
        var handler = new HttpClientHandler
        {
            Proxy = new System.Net.WebProxy($"http://50.114.105.39:50100"),
            DefaultProxyCredentials = new System.Net.NetworkCredential("mephisto123", "b9je9X7hGA")
        };

        var payload = new
        {
            model = _model,
          
            messages = new[]
            {
                new { role = "user", content = _prompt }
            }
        };
        var jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        using (var httpClient = new HttpClient(handler))
        {
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Chat.Api_key}");
            try
            {
                var response = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions",content); // Обновите URL, если необходимо
                                                                                                                  //Logger.SavePayLoad(JsonConvert.SerializeObject(response, Formatting.Indented));

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var responseObject = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(jsonResponse);
                    return responseObject.choices[0].message.content;

                }
            }
            catch (Exception ex) { Logger.AddLog($"Error details: " + ex.Message); }
        }
        return "";
    }
    public static async Task<dynamic> AskOpenAI_formatted_response(string _prompt, FilesManager.Create c, double chatid =-1)
    {
        try
        { 
            var handler = new HttpClientHandler
            {
                Proxy = new System.Net.WebProxy($"http://50.114.105.39:50100"),
                DefaultProxyCredentials = new System.Net.NetworkCredential("mephisto123", "b9je9X7hGA")
            };

            var payload = new
            {
                model = "gpt-4o-2024-08-06",                
                messages = new[]
      {
        new { role = "system", content = "Ты лучший в мире веб программист. Помоги пользователю создать то, что он просит." },
        new { role = "user", content = _prompt }
    },
                response_format = new
                {
                    type = "json_schema",
                    json_schema = new
                    {
                        name = "response_formats",
                        strict = true,
                        schema = new
                        {
                            type = "object",
                            properties = new
                            {
                                files = new
                                {
                                    type = "array",
                                    items = new
                                    {
                                        type = "object",
                                        properties = new
                                        {
                                            file_name = new { type = "string", description = "The name of the file including extension." },
                                            file_content = new { type = "string", description = "The content of the file as a string, ready to be written to a file." },
                                            file_type = new { type = "string", description = "The type or format of the file (e.g., 'html', 'css', 'json')." }
                                        },
                                        required = new string[] { "file_name", "file_content", "file_type" },
                                        additionalProperties = false
                                    }
                                }
                            },
                            required = new[] { "files" },
                            additionalProperties = false
                        }
                    }
                }
            };

            var jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            using (var httpClient = new HttpClient(handler))
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Chat.Api_key}");

                var response = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content); // Обновите URL, если необходимо
                                                                                                                  //Logger.SavePayLoad(JsonConvert.SerializeObject(response, Formatting.Indented));

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    dynamic responseObject = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(jsonResponse);
                    return responseObject.choices[0].message.content;

                }
                else
                {
                    if (chatid != -1) await Chat.Bot.SendTextMessageAsync((int)chatid, "Ошибка при обращении к OpenAI. Попробуйте позже.");
                }
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error details: " + ex.Message);
            if (chatid != -1) await Chat.Bot.SendTextMessageAsync((int)chatid, "Ошибка при обращении к OpenAI. Попробуйте позже."+ ex.Message);
        }
        return "";
    }

    public static async Task<float[]> AskOpenAI2Embedding(string _prompt)
    {
        await Task.Delay(10);
        var handler = new HttpClientHandler
        {
            Proxy = new System.Net.WebProxy($"http://50.114.105.39:50100"),
            DefaultProxyCredentials = new System.Net.NetworkCredential("mephisto123", "b9je9X7hGA")
        };

        var payload = new
        {
            model = "text-embedding-ada-002",
           // model = "text-embedding-3-large",
            input = _prompt
        };
        var jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        using (var httpClient = new HttpClient(handler))
        {
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Chat.Api_key}");
            try
            {
                var response = await httpClient.PostAsync("https://api.openai.com/v1/embeddings", content); // Обновите URL, если необходимо
                                                                                                            //Logger.SavePayLoad(JsonConvert.SerializeObject(response, Formatting.Indented));

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var responseObject = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(jsonResponse);

                    // Извлекаем массив эмбеддингов из data
                    var embeddingArray = responseObject["data"][0]["embedding"].ToObject<float[]>(); // Преобразуем JArray в float[]

                    return (embeddingArray);
                }
            }
            catch (Exception ex) { Logger.AddLog($"Error details: " + ex.Message); }
        }
        return new float[] {};
    }
    public static async Task<List<AssistantFunction>> AskOpenAI_Activate_Functions(string _prompt)
    {
        var assistantFunctions = new List<AssistantFunction>();

        try
        {
            var handler = new HttpClientHandler
            {
                Proxy = new System.Net.WebProxy($"http://50.114.105.39:50100"),
                DefaultProxyCredentials = new System.Net.NetworkCredential("mephisto123", "b9je9X7hGA")
            };

            var messages = new[]
            {
            new
            {
                role = "system",
                content = @"Ты БОТ, к тебе обращается пользователь из чата. Определи смысл запроса и, исходя из этого, сформируй последовательность действий в формате JSON, используя следующую схему:

- actions: массив действий, где каждое действие представляет вызов функции.
  - function: имя функции для вызова. Возможные значения: 'GenerateImages', 'JustAnswer'.
  - parameters: объект с параметрами для функции.

Вот описание доступных функций:

1. **GenerateImages**
   - **Описание:** Генерирует изображения на основе запросов пользователя.
   - **Параметры:**
     - count (integer, max 10): Количество изображений для генерации.
     - imagePrompts (array of strings): Массив промптов для генерации изображений.

2. **JustAnswer**
   - **Описание:** Предоставляет текстовый ответ на запрос пользователя.
   - **Параметры:**
     - prompt (string): Ответ на запрос пользователя.

Верни ответ строго в формате JSON, соответствующий указанной схеме."
            },
            new { role = "user", content = _prompt }
        };

            var response_format = new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = "actions_schema",
                    strict = true,
                    schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            actions = new
                            {
                                type = "array",
                                items = new
                                {
                                    type = "object",
                                    properties = new
                                    {
                                        function = new
                                        {
                                            type = "string",
                                            description = "The name of the function to call.",
                                            @enum = new[] { "GenerateImages", "JustAnswer" }
                                        },
                                        parameters = new
                                        {
                                            type = "object",
                                            additionalProperties = true
                                        }
                                    },
                                    required = new[] { "function", "parameters" },
                                    additionalProperties = false
                                }
                            }
                        },
                        required = new[] { "actions" },
                        additionalProperties = false
                    }
                }
            };

            var payload = new
            {
                model = "gpt-4o-2024-08-06",
                messages = messages,
                response_format = response_format
            };

            var jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            using (var httpClient = new HttpClient(handler))
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Chat.Api_key}");

                var response = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    dynamic responseObject = JsonConvert.DeserializeObject<dynamic>(jsonResponse);

                    var assistantMessage = responseObject.choices[0].message;

                    if (assistantMessage.content != null)
                    {
                        // Парсим JSON-ответ
                        string contentResponse = assistantMessage.content.ToString();
                        dynamic actionsResponse = JsonConvert.DeserializeObject<dynamic>(contentResponse);

                        // Обрабатываем каждое действие и добавляем его в список
                        foreach (var action in actionsResponse.actions)
                        {
                            string functionName = action.function;
                            JObject parameters = action.parameters;

                            var functionParameters = parameters.ToObject<Dictionary<string, object>>();

                            assistantFunctions.Add(new AssistantFunction
                            {
                                FunctionName = functionName,
                                Parameters = functionParameters
                            });
                        }

                        return assistantFunctions;
                    }
                    else
                    {
                        // Если контент отсутствует, возвращаем пустой список
                        return assistantFunctions;
                    }
                }
                else
                {
                    // В случае ошибки API возвращаем пустой список
                    return assistantFunctions;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error details: " + ex.Message);
            // В случае исключения возвращаем пустой список
            return assistantFunctions;
        }
    }
    public class AssistantFunction
    {
        public string FunctionName { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
    }
}