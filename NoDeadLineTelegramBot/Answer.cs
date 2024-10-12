using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot; 
using System.Diagnostics;

public static class Answer
{
    // Словарь для хранения количества генераций для каждого сервера
    private static Dictionary<string, int> serverGenerationCounts = new Dictionary<string, int>();

    public static async Task<bool> CognitiveAnswer(Message message)
    {
        // Сразу отправляем статусное сообщение о начале обработки
        var statusMessage = await Chat.Bot.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"Запрос получен. Начинаю обработку..."
        );

        // Запускаем таймер
        var stopwatch = Stopwatch.StartNew();

        // Выполняем запрос на получение функций
        var assistantFunctions = await OpenAIClient.AskOpenAI_Activate_Functions(message.Text);

        if (assistantFunctions.Count == 0)
        {
            await Chat.Bot.EditMessageTextAsync(
                chatId: message.Chat.Id,
                messageId: statusMessage.MessageId,
                text: "Функция не распознана."
            );
            return false;
        }

        // Составляем информацию о функциях для статусного сообщения
        string functionSummary = $"Обнаружено {assistantFunctions.Count} функций: ";
        functionSummary += string.Join(", ", assistantFunctions.Select(f => f.FunctionName));

        // Обновляем статусное сообщение, добавляя информацию о функциях
        await Chat.Bot.EditMessageTextAsync(
            chatId: message.Chat.Id,
            messageId: statusMessage.MessageId,
            text: $"Начинаю обработку запроса...\n{functionSummary}"
        );

        int totalPromptChars = 0;
        int totalResponseChars = 0;

        foreach (var assistantFunction in assistantFunctions)
        {
            if (assistantFunction.FunctionName == "GenerateImages")
            {
                int count = Convert.ToInt32(assistantFunction.Parameters["count"]);
                var imagePrompts = ((JArray)assistantFunction.Parameters["imagePrompts"]).ToObject<List<string>>();

                totalPromptChars += imagePrompts.Sum(prompt => prompt.Length);

                // Инициализация счетчиков серверов
                foreach (var server in ComfyUI_adapter.AvailableServers)
                {
                    if (!serverGenerationCounts.ContainsKey(server.Name))
                    {
                        serverGenerationCounts[server.Name] = 0;
                    }
                }

                // Обновляем статусное сообщение перед началом генерации изображений
                await Chat.Bot.EditMessageTextAsync(
                    chatId: message.Chat.Id,
                    messageId: statusMessage.MessageId,
                    text: $"Генерирую {count} изображений по следующим промптам:\nГотово [0 из {count}]\nОбщее время: {stopwatch.Elapsed.TotalSeconds:F2} секунд"
                );

                // Вызов функции генерации и отправки изображений
                await GenerateImagesByFunctions(message, count, imagePrompts, statusMessage, stopwatch);
            }
            else if (assistantFunction.FunctionName == "JustAnswer")
            {
                string promptResponse = assistantFunction.Parameters["prompt"].ToString();

                totalPromptChars += promptResponse.Length;

                // Обновляем статусное сообщение перед отправкой текста
                await Chat.Bot.EditMessageTextAsync(
                    chatId: message.Chat.Id,
                    messageId: statusMessage.MessageId,
                    text: $"Отправляю текстовый ответ...\nТекущий промпт: {promptResponse}\nОбщее время: {stopwatch.Elapsed.TotalSeconds:F2} секунд"
                );
                await Task.Delay(5500);
                await Chat.Bot.SendTextMessageAsync(message.Chat.Id, promptResponse);

                totalResponseChars += promptResponse.Length;

                await Chat.Bot.EditMessageTextAsync(
                    chatId: message.Chat.Id,
                    messageId: statusMessage.MessageId,
                    text: $"Ответ отправлен.\nОбщее время: {stopwatch.Elapsed.TotalSeconds:F2} секунд"
                );
            }
        }

        stopwatch.Stop();
        var elapsedTime = stopwatch.Elapsed;

        string finalStatus = $"Все функции выполнены.\n" +
                             $"Затраченное время: {elapsedTime.TotalSeconds:F2} секунд\n" +
                             $"Количество символов в промптах: {totalPromptChars}\n" +
                             $"Количество символов в ответах: {totalResponseChars}";

        await Chat.Bot.EditMessageTextAsync(
            chatId: message.Chat.Id,
            messageId: statusMessage.MessageId,
            text: finalStatus
        );

        return true;
    }

    private static async Task<string> GenerateImageAndUpdateStatus(string prompt, Message message, Server server, Message statusMessage, Stopwatch stopwatch, int currentIndex, int totalCount)
    {
        try
        {
            string filePath = await ComfyUI_adapter.GenerateImage(prompt, message, server.Address);

            if (!System.IO.File.Exists(filePath) || new FileInfo(filePath).Length == 0)
            {
                Console.WriteLine($"Первичная попытка не удалась для промпта: '{prompt}'. Повторная попытка...");

                filePath = await ComfyUI_adapter.GenerateImage(prompt, message, server.Address);
                if (!System.IO.File.Exists(filePath) || new FileInfo(filePath).Length == 0)
                {
                    await Chat.Bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: $"Ошибка: файл изображения по запросу '{prompt}' не был создан или пустой. Пропускаю этот файл."
                    );
                    return null;
                }
            }

            long fileSize = new FileInfo(filePath).Length;
            if (fileSize < 1000)
            {
                await Chat.Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Ошибка: файл изображения '{filePath}' слишком мал ({fileSize} байт). Пропускаю этот файл."
                );
                return null;
            }

            var stream = System.IO.File.OpenRead(filePath);

            var mediaItem = new InputMediaPhoto(InputFile.FromStream(stream, Path.GetFileName(filePath)))
            {
                Caption = $"Промпт: {prompt}\nРазмер файла: {fileSize / 1024} KB"
            };

            if (serverGenerationCounts.ContainsKey(server.Name))
            {
                serverGenerationCounts[server.Name]++;
            }

            await UpdateServerStatusAndProgress(message, statusMessage, totalCount, currentIndex, stopwatch);

            return filePath;
        }
        catch (Exception ex)
        {
            await Chat.Bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"Произошла ошибка при обработке изображения '{prompt}': {ex.Message}"
            );
            return null;
        }
    }

    private static async Task UpdateServerStatusAndProgress(Message message, Message statusMessage, int totalCount, int currentIndex, Stopwatch stopwatch)
    {
        var serverStatuses = new List<string>();
        foreach (var server in ComfyUI_adapter.AvailableServers)
        {
            int queueSize = await server.GetQueueSize();
            int generationCount = serverGenerationCounts.ContainsKey(server.Name) ? serverGenerationCounts[server.Name] : 0;

            string status = queueSize >= 0
                ? $"Сервер {server.Name} (GPU: {server.GPU}) - Очередь: {queueSize}, Генераций: {generationCount}"
                : $"Сервер {server.Name} (GPU: {server.GPU}) - Недоступен";

            serverStatuses.Add(status);
        }

        string newStatusText = $"Текущая загруженность серверов:\n" + string.Join("\n", serverStatuses) +
                               $"\nГенерирую {totalCount} изображений. Готово [{currentIndex} из {totalCount}]. Общее время: {stopwatch.Elapsed.TotalSeconds:F2} секунд.";

        try
        {
            // Проверяем, изменился ли статус перед отправкой
            if (statusMessage.Text != newStatusText)
            {
                await Chat.Bot.EditMessageTextAsync(
                    chatId: message.Chat.Id,
                    messageId: statusMessage.MessageId,
                    text: newStatusText
                );
            }
        }
        catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.Message.Contains("message is not modified"))
        {
            Console.WriteLine("Статусное сообщение не изменилось, пропускаем обновление.");
        }
        catch (Exception ex)
        {
            Logger.AddLog($"Ошибка обновления статуса: {ex.Message}");
        }
    }
}
