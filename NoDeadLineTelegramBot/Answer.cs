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

                // Подсчитываем количество символов в промптах
                totalPromptChars += imagePrompts.Sum(prompt => prompt.Length);

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

                // Подсчитываем количество символов в промпте
                totalPromptChars += promptResponse.Length;

                // Обновляем статусное сообщение перед отправкой текста
                await Chat.Bot.EditMessageTextAsync(
                    chatId: message.Chat.Id,
                    messageId: statusMessage.MessageId,
                    text: $"Отправляю текстовый ответ...\nТекущий промпт: {promptResponse}\nОбщее время: {stopwatch.Elapsed.TotalSeconds:F2} секунд"
                );
                await Task.Delay(5500);
                await Chat.Bot.SendTextMessageAsync(message.Chat.Id, promptResponse);

                // Подсчитываем количество символов в ответе
                totalResponseChars += promptResponse.Length;

                // Обновляем статусное сообщение после отправки ответа
                await Chat.Bot.EditMessageTextAsync(
                    chatId: message.Chat.Id,
                    messageId: statusMessage.MessageId,
                    text: $"Ответ отправлен.\nОбщее время: {stopwatch.Elapsed.TotalSeconds:F2} секунд"
                );
            }
        }

        // Останавливаем таймер
        stopwatch.Stop();
        var elapsedTime = stopwatch.Elapsed;

        // Обновляем статусное сообщение после выполнения всех функций
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

    public static async Task GenerateImagesByFunctions(Message message, int count, List<string> imagePrompts, Message statusMessage, Stopwatch stopwatch)
    {
        try
        {
            var media = new List<IAlbumInputMedia>();
            var streams = new List<Stream>(); // Список для хранения открытых потоков

            int batchSize = 10; // Ограничение на количество изображений за одну отправку
            int totalBatches = (int)Math.Ceiling((double)count / batchSize);
            int generatedImages = 0;

            // Если количество промптов меньше, чем count, создаём расширенный список промптов, заполняя его циклически
            var extendedPrompts = new List<string>();
            for (int i = 0; i < count; i++)
            {
                extendedPrompts.Add(imagePrompts[i % imagePrompts.Count]);
            }

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                int startIndex = batchIndex * batchSize;
                int endIndex = Math.Min(startIndex + batchSize, count);
                media.Clear();
                streams.Clear();

                for (int i = startIndex; i < endIndex; i++)
                {
                    string promptValue = extendedPrompts[i];

                    // Генерация изображения с использованием ComfyUI_adapter
                    string filePath;
                    try
                    {
                        filePath = await ComfyUI_adapter.GenerateImage(promptValue);

                        // Проверка существования и размера файла
                        if (!System.IO.File.Exists(filePath) || new FileInfo(filePath).Length == 0)
                        {
                            await Chat.Bot.SendTextMessageAsync(
                                chatId: message.Chat.Id,
                                text: $"Ошибка: файл изображения по запросу '{promptValue}' не был создан или пустой. Пропускаю этот файл."
                            );
                            continue;
                        }

                        // Проверка формата изображения и, при необходимости, конвертация в поддерживаемый формат
                        string extension = Path.GetExtension(filePath).ToLower();
                        if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")
                        {
                            await Chat.Bot.SendTextMessageAsync(
                                chatId: message.Chat.Id,
                                text: $"Ошибка: файл изображения '{filePath}' имеет неподдерживаемый формат '{extension}'. Пропускаю этот файл."
                            );
                            continue;
                        }

                        // Проверка разрешения изображения
                        using (var image = System.Drawing.Image.FromFile(filePath))
                        {
                            if (image.Width > 4096 || image.Height > 4096)
                            {
                                await Chat.Bot.SendTextMessageAsync(
                                    chatId: message.Chat.Id,
                                    text: $"Ошибка: изображение '{filePath}' превышает допустимое разрешение 4096x4096. Пропускаю этот файл."
                                );
                                continue;
                            }
                        }
                    }
                    catch (TimeoutException)
                    {
                        await Task.Delay(5500); // Задержка перед отправкой сообщения
                        await Chat.Bot.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: $"Изображение по запросу '{promptValue}' не стало доступным в течение заданного времени."
                        );
                        continue;
                    }
                    catch (Exception ex)
                    {
                        await Chat.Bot.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: $"Произошла ошибка при обработке изображения '{promptValue}': {ex.Message}"
                        );
                        continue;
                    }

                    // Открываем поток для изображения
                    var stream = System.IO.File.OpenRead(filePath);
                    streams.Add(stream);

                    // Создаем InputMediaPhoto и добавляем в коллекцию media
                    media.Add(new InputMediaPhoto(InputFile.FromStream(stream, Path.GetFileName(filePath)))
                    {
                        Caption = promptValue
                    });

                    // Обновляем статусное сообщение после генерации каждого изображения
                    generatedImages++;
                    await Task.Delay(5500); // Задержка перед обновлением сообщения
                    await Chat.Bot.EditMessageTextAsync(
                        chatId: message.Chat.Id,
                        messageId: statusMessage.MessageId,
                        text: $"Генерирую {count} изображений по следующим промптам:\n" +
                              $"Текущий промпт: {promptValue}\n" +
                              $"Готово [{generatedImages} из {count}]\n" +
                              $"Общее время: {stopwatch.Elapsed.TotalSeconds:F2} секунд"
                    );
                }

                // Отправляем текущую партию изображений в Telegram с обработкой ошибок
                bool retry;
                do
                {
                    retry = false;
                    try
                    {
                        Console.WriteLine($"Start Send Gallery for batch {batchIndex + 1}");
                        await Task.Delay(5500); // Задержка перед отправкой группы изображений
                        var messages = await Chat.Bot.SendMediaGroupAsync(
                            chatId: message.Chat.Id,
                            media: media.ToArray(),
                            cancellationToken: default
                        );
                    }
                    catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.Message.Contains("Too Many Requests"))
                    {
                        // Если мы получаем ошибку "Too Many Requests", нужно дождаться указанного времени
                        int retryAfter = 5; // Установка времени по умолчанию

                        // Пытаемся извлечь время ожидания из сообщения об ошибке
                        var match = System.Text.RegularExpressions.Regex.Match(ex.Message, @"retry after (\d+)");
                        if (match.Success && int.TryParse(match.Groups[1].Value, out int waitTime))
                        {
                            retryAfter = waitTime;
                        }

                        Console.WriteLine($"Too many requests. Retrying after {retryAfter} seconds.");
                        await Task.Delay((retryAfter + 1) * 1000); // Задержка с увеличенным временем
                        retry = true; // Устанавливаем флаг, чтобы повторить отправку
                    }
                    catch (Exception ex)
                    {
                        Logger.AddLog("Exception while sending gallery: " + ex.Message);
                        await Task.Delay(5500); // Задержка перед отправкой сообщения
                        await Chat.Bot.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: $"Ошибка при отправке галереи: {ex.Message}\nСтек вызовов: {ex.StackTrace}"
                        );
                    }
                    finally
                    {
                        // Закрываем все потоки
                        foreach (var stream in streams)
                        {
                            stream.Dispose();
                        }
                    }
                } while (retry);

                // Задержка между отправками для избежания ограничений API Telegram
                await Task.Delay(5500);
            }

            // Обновляем статусное сообщение после завершения всех операций
            await Task.Delay(5500); // Задержка перед обновлением сообщения
            await Chat.Bot.EditMessageTextAsync(
                chatId: message.Chat.Id,
                messageId: statusMessage.MessageId,
                text: "Все изображения успешно сгенерированы и отправлены.\n" +
                      $"Общее время: {stopwatch.Elapsed.TotalSeconds:F2} секунд"
            );
        }
        catch (Exception ex)
        {
            Logger.AddLog($"Error in GenerateImagesByFunctions: {ex.Message}\n{ex.StackTrace}");
            await Task.Delay(5500); // Задержка перед отправкой сообщения
            await Chat.Bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"Произошла ошибка во время генерации изображений: {ex.Message}\n" +
                      $"Стек вызовов: {ex.StackTrace}"
            );
        }
    }



}
