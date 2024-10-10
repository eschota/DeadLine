using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;

 
    public static class Answer
    {
        public static async Task <bool> CognitiveAnswer(Message message)
        {
        var assistantFunctions = await OpenAIClient.AskOpenAI_Activate_Functions(message.Text);

        if(assistantFunctions.Count==0) await Chat.Bot.SendTextMessageAsync((int)message.Chat.Id, "функция не распознана");
        foreach (var assistantFunction in assistantFunctions)
        {
            if (assistantFunction.FunctionName == "GenerateImages")
            {
                int count = Convert.ToInt32(assistantFunction.Parameters["count"]);
                var imagePrompts = ((JArray)assistantFunction.Parameters["imagePrompts"]).ToObject<List<string>>();
                await GenerateImagesByFunctions(message, count, imagePrompts);
                await Chat.Bot.SendTextMessageAsync((int)message.Chat.Id, "Типа пиздатые картинки по запросу");
                // Вызов вашей функции генерации изображений
                //await GenerateImages(count, imagePrompts, chatid);
            }
            else if (assistantFunction.FunctionName == "JustAnswer")
            {
                string promptResponse = assistantFunction.Parameters["prompt"].ToString();
                await Chat.Bot.SendTextMessageAsync((int)message.Chat.Id, promptResponse);
            }
        }
        return true;
        }
    public static async Task GenerateImagesByFunctions(Message message, int count, List<string> imagePrompts)
    {
        try
        {
            var media = new List<IAlbumInputMedia>();
            var streams = new List<Stream>(); // Список для хранения открытых потоков

            int batchSize = 10; // Ограничение на количество изображений за одну отправку
            int totalBatches = (int)Math.Ceiling((double)count / batchSize);

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                int startIndex = batchIndex * batchSize;
                int endIndex = Math.Min(startIndex + batchSize, count);
                media.Clear();
                streams.Clear();

                for (int i = startIndex; i < endIndex; i++)
                {
                    string promptValue = imagePrompts[i];

                    // Генерация изображения с использованием ComfyUI_adapter
                    string filePath = await ComfyUI_adapter.GenerateImage(promptValue);

                    // Открываем поток для изображения
                    var stream = System.IO.File.OpenRead(filePath);
                    streams.Add(stream); // Добавляем поток в список, чтобы потом корректно закрыть

                    // Создаем InputMediaPhoto и добавляем в коллекцию media
                    if (i == startIndex)
                        media.Add(new InputMediaPhoto(InputFile.FromStream(streams.Last(), Path.GetFileName(filePath))) { Caption = promptValue });
                    else
                        media.Add(new InputMediaPhoto(InputFile.FromStream(streams.Last(), Path.GetFileName(filePath))));
                }

                // Отправляем текущую партию изображений в Telegram
                try
                {
                    Console.WriteLine($"Start Send Gallery for batch {batchIndex + 1}");
                    var messages = await Chat.Bot.SendMediaGroupAsync(
                        chatId: message.Chat.Id,
                        media: media.ToArray(),
                        cancellationToken: default
                    );
                }
                catch (Exception ex)
                {
                    Logger.AddLog("Exception while sending gallery: " + ex.Message);
                }
                finally
                {
                    // Закрываем все потоки
                    foreach (var stream in streams)
                    {
                        stream.Dispose();
                    }
                }

                // Задержка между отправками для избежания ограничений API Telegram
                await Task.Delay(500);
            }
        }
        catch (Exception ex)
        {
            Logger.AddLog("Error in GenerateImagesByFunctions: " + ex.Message);
        }
    }

}