using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.IO;
using System;
using System.Diagnostics;
using System.Runtime;

using System.Text.RegularExpressions;
using Microsoft.VisualBasic;
using System.Threading;
using System.Text;
using Newtonsoft.Json;
using System.Linq.Expressions;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Linq;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Newtonsoft.Json.Linq;
using Telegram.Bot.Requests;
using Telegram.Bot.Types.ReplyMarkups;
internal static class Chat
{
    internal static TelegramBotClient Bot;
    static HashSet<long> chatIds = new HashSet<long>();
    public static int LastMessageID = 0;
    private static async Task TelegramUpdate(ITelegramBotClient client, Update update, CancellationToken token)
    {
        try
        {
            //Console.WriteLine($"update.Type: {update.Type}: update.Message.Chat.Id {update.Message.Chat.Id}:update.Message {update.Message.Text}");
            await Buttons.HandleUpdateAsync(update);
            if (update.Type == UpdateType.Message)
            {
                bool isAudio = update.Message.Type == MessageType.Voice;
                bool isGroupChat = update.Message.Chat.Type != ChatType.Private;
                bool containsBotKeyword = update.Message.Text != null && Regex.IsMatch(update.Message.Text.ToLower(), @"\bбот\b");
                bool containsBotKeywordinCaption = update.Message.Caption != null && Regex.IsMatch(update.Message.Caption.ToLower(), @"\bбот\b");

                bool containSite = (update.Message.Text != null && update.Message.Text.Length > 3 &&
                     Regex.IsMatch(update.Message.Text.ToLower(), @"\b(создай|сайт|сгенерируй|страницу)\b")) ||
                    (update.Message.Caption != null && update.Message.Caption.Length > 3 &&
                     Regex.IsMatch(update.Message.Caption.ToLower(), @"\b(создай|сайт|сгенерируй|страницу)\b"));

                bool containsChat = (update.Message.Text != null && update.Message.Text.Length > 3 &&
                      Regex.IsMatch(update.Message.Text.ToLower(), @"\b(чата|чат|чате|чату)\b")) ||
                     (update.Message.Caption != null && update.Message.Caption.Length > 3 &&
                      Regex.IsMatch(update.Message.Caption.ToLower(), @"\b(чата|чат|чате|чату)\b"));


                if (update.Message.Type == MessageType.Photo) if (containsBotKeywordinCaption) { UpdateChatID(update.Message, containsChat); SaveMessageToHistory(update.Message); return; }


                if (containSite && containsBotKeyword)
                {
                    GenerateSite(update.Message, containsChat);
                    SaveMessageToHistory(update.Message);
                }
                else
            if (!isGroupChat || containsBotKeyword || containsBotKeywordinCaption || isAudio)
                {
                    UpdateChatID(update.Message, containsChat);
                    SaveMessageToHistory(update.Message);
                }
                else
                {
                    SaveMessageToHistory(update.Message);
                }
            }

        }
        catch { }
    }
    public static string GetChatHistoryFiles(string chatHistoryPath, int MessageCount)
    {

        if (!Directory.Exists(chatHistoryPath))
        {
            Logger.AddLog("No history available.");
            return "";
        }

        // Get all files in the directory, ordered by creation time (oldest first)
        var allFiles = Directory.GetFiles(chatHistoryPath)
            .Select(filePath => new FileInfo(filePath))
            .OrderBy(fileInfo => fileInfo.CreationTime)
            .ToList();

        // Ensure MessageCount does not exceed the number of files
        if (MessageCount > allFiles.Count)
        {
            MessageCount = allFiles.Count;
        }

        // Take the latest 'MessageCount' files
        var fileContents = allFiles
            .TakeLast(MessageCount)
            .Select(fileInfo => System.IO.File.ReadAllText(fileInfo.FullName));

        // Concatenate all file contents
        return string.Join(Environment.NewLine, fileContents).ToLower().Replace("нарисуй", "");
    }

    static void SaveMessageToHistory(Message message)
    {

        if (message.From.IsBot) return;
        //
        bool isGroupChat = message.Chat.Type != ChatType.Private;
        string chatHistoryPath = "";

        if (isGroupChat)
            chatHistoryPath = Path.Combine(Paths.Chats, "history_" + message.Chat.Title + ".json");
        else
            chatHistoryPath = Path.Combine(GetChatPath(message), "history.json");

        string directoryPath = Path.GetDirectoryName(chatHistoryPath);
        if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);


        string messageContent = $"{message.From.Username}: {message.Text}\n";


        using (FileStream fs = new FileStream(chatHistoryPath, FileMode.Append, FileAccess.Write))
        {
            using (StreamWriter writer = new StreamWriter(fs))
            {
                writer.Write(messageContent);
            }
        }


        LimitLinesInFile(chatHistoryPath, 1000);
    }

    // Метод для ограничения количества строк в файле
    static void LimitLinesInFile(string filePath, int maxLines)
    {
        var lines = System.IO.File.ReadAllLines(filePath);
        if (lines.Length > maxLines)
        {
            // Сохраняем только последние maxLines строк
            System.IO.File.WriteAllLines(filePath, lines.Skip(lines.Length - maxLines));
        }
    }


    static internal string GetResources(string ask)
    {
        string usePath = "";
        if (ask.ToLower().Contains("используй "))
        {
            try
            {
                // Extract the relative path
                int startIndex = ask.ToLower().IndexOf("используй ") + "используй ".Length;
                int endIndex = ask.IndexOf(' ', startIndex);
                if (endIndex == -1) endIndex = ask.Length;
                usePath = ask.Substring(startIndex, endIndex - startIndex).Trim();
                // Combine the relative path with the base path
                string fullPath = Path.Combine(Paths.Sites, usePath);

                // Check if the directory exists
                if (Directory.Exists(fullPath))
                {
                    // Get all png and jpg files
                    var files = Directory.GetFiles(fullPath, "*.*", SearchOption.AllDirectories)
                                         .Where(file => file.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                                         .ToList();

                    // Combine the file names into a single string
                    string combinedFiles = string.Join(", ", files.Select(file => $"https://renderfin.com/sites/{usePath}/{Path.GetFileName(file)}"));

                    // Use combinedFiles as needed, for example, log it or use it in further processing
                    Logger.AddLog($"Combined files: {combinedFiles}");
                }
                else
                {
                    Logger.AddLog($"Directory does not exist: {fullPath}");
                }
            }
            catch (Exception ex)
            {
                Logger.AddLog($"Exception: {ex.Message}");
            }
        }
        return usePath;


    }

    static internal async void GenerateSite(Message message, bool withStory)
    {
        var stopwatch = Stopwatch.StartNew();
        string ask = $"";
        if (message.Text != null)
            ask += message.Text;
        else if (message.Caption != null)
            ask += message.Caption;

        string usePath = "";
        usePath = GetResources(ask);



        ask = $"[{ask}]" + " По этому запросу определи название сайта и используй его как переменную {siteName}. Твоя задача сгенерировать полностью рабочую веб страницу виде {siteName}.html, и соответствующий стиль {siteName}.css (здесь ты должен заменить на имя которое у .html), на выходе должно получиться 2 полностью рабочих файла, чтобы сайт работал. Ответ выдай в формате JSON, правильно экранированном для передачи в текстовом виде, содержащий 3 строки {siteName},{html},{css} , не добавляй лишние символы, нужно чтобы этот JSON файл легко десериализовался.";
        if (withStory) { ask += ". Для понимания контекста используй историю из этого чата: \n[" + GetChatHistoryFiles(Path.Combine(Paths.Chats, "history_" + message.Chat.Title), 50) + "]\n"; };
        if (usePath != "") ask += "\n Можешь пользоваться предложенным списком файлов, который доступны статично по указанным ссылкам:  " + usePath + "добавляй в начало пути каждого используемого файла ../ чтобы сделать путь относительным родительской папки, используй точные названия файлов ";


        if (!chatIds.Contains(message.Chat.Id))
        {
            RegisterChat(message);
        }

        List<Message> m = new List<Message>();



        if (message.Type == MessageType.Text)
        {
            m.Add(await GenerateSiteAnswerToTelegram(message, stopwatch, ask, withStory));

        }


        // foreach (var item in m)
        try
        {
            await Bot.DeleteMessageAsync(m[0].Chat.Id, m[0].MessageId);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }





    }
    /// <summary>
    /// /////
    /// </summary>
    /// <param name="message"></param>
    static internal async void UpdateChatID(Message message, bool withStory)
    {
        var stopwatch = Stopwatch.StartNew();
        string ask = "";// "$"{message.From.FirstName};
        if (message.Text != null)
            ask += message.Text;
        else if (message.Caption != null)
            ask += message.Caption;

        ask = ask.ToLower();
        if (ask.ToLower().Contains("нарисуй"))
        {
            Message currentMessage = null;
            ask = ask.ToLower().Replace("нарисуй", "");
            if (ask.ToLower().Contains("бот")) ask = ask.ToLower().Replace("бот", "");
            string fName = $"{DateTime.Now:yyyyMMdd_HHmmssfff}.png";
            string filePath = Path.Combine(Paths.Imagine, fName);
            string askGPT = "";

            if (!ask.Contains("промпт") && !ask.Contains("промт"))
            {
                currentMessage = await SendTextMessage(message.Chat.Id, Phrases.GetRandomPhrase());


                askGPT = "Сгенерируй промпт для StableDiffsion на английском языке, используй профессиональный подход к созданию промпта. Следующий в скобках Текст  нужно превратить в промпт: [" + ask + "] Если не можешь сделать промпт просто переведи с русского на английский.  \n Отвечай в формате .JSON, поле ответа \"answer\": ";
                if (ask.Contains("порно "))
                {
                    askGPT = askGPT.Replace("порно ", "");

                    askGPT = await SimpleRequest(askGPT);

                    try
                    {
                        JObject jObject = JObject.Parse(askGPT);
                        askGPT = jObject["answer"].ToString();

                    }
                    catch { }
                    EditTextMessage(currentMessage, askGPT);
                    await StableDiffusion.StableDiffusionTxtToImage(askGPT, filePath, 1);
                }
                else
                {
                    askGPT = await SimpleRequest(askGPT);

                    try
                    {
                        if (askGPT.Contains("```json")) askGPT = askGPT.Replace("```json", "");
                        if (askGPT.Contains("``` ")) askGPT = askGPT.Substring(0, askGPT.IndexOf("``` "));
                        JObject jObject = JObject.Parse(askGPT);
                        askGPT = jObject["answer"].ToString();

                    }
                    catch { }
                    EditTextMessage(currentMessage, askGPT);

                    await StableDiffusion.StableDiffusionTxtToImage(askGPT, filePath, 0);
                    DeleteTextMessage(currentMessage);

                }

            }
            else
            {
                if (ask.Contains("промпт")) ask = ask.Replace("промпт", "");
                if (!ask.Contains("промт")) ask = ask.Replace("промт", "");
                await StableDiffusion.StableDiffusionTxtToImage(ask, filePath, 0);
            }

            if (System.IO.File.Exists(Path.Combine(Paths.Imagine, fName)))
            {
                await SendPhotoMessage(message.Chat.Id, Path.Combine(Paths.Imagine, fName), ask, "@"+message.From.Username, askGPT);
                return;
            }
            else
            {
                await Bot.SendTextMessageAsync(message.Chat.Id, "хер тебе попрошайка!" + $" *{stopwatch.Elapsed.TotalSeconds.ToString("00")}s*", parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                Logger.AddLog($"File not found: {filePath}");
            }

        }
        else
        if (withStory)
        {
            ask += ". Для понимания контекста используй историю из этого чата: \n[" + GetChatHistoryFiles(Path.Combine(Paths.Chats, "history_" + message.Chat.Title), 100) + "]\nЕсли есть затруднения с ответом, обязательно напиши причины, не отправляй никгда пустой ответ, и старайся отвечать кратко, но не превышая никогда 3500 символов на ответ.";
        }
        else
        {
            //   ask += ". Для понимания контекста используй историю из этого чата: \n[" + GetChatHistoryFiles(Path.Combine(Paths.Chats, "history_" + message.Chat.Title),30) + "]\n Но сконцентрируйся на самом первом запросе пользователя. Если есть затруднения с ответом, обязательно напиши причины, не отправляй никгда пустой ответ, и старайся отвечать кратко, но не превышая никогда 3500 символов на ответ.";
        }



        if (!chatIds.Contains(message.Chat.Id))
        {
            RegisterChat(message);
        }

        List<Message> m = new List<Message>();

        if (message.Type == MessageType.Photo)
        {
            m.Add(await Bot.SendTextMessageAsync(message.Chat.Id, "Распознаю фото, 30 сек.", parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown));
            string imagePath = await LoadImage(message);
            byte[] questionBytes = System.Text.Encoding.UTF8.GetBytes("Что на этой картинке?");

            byte[] compressedQuestionBytes = CompressGzip(questionBytes);
            string compressedQuestionBase64 = Convert.ToBase64String(compressedQuestionBytes);

            string mes = "";
            if (!System.String.IsNullOrEmpty(message.Caption))
            {
                if (message.Caption.ToLower().Contains("переделай"))
                {
                    mes = Clear(message.Caption, new string[] { "переделай", "бот", "нарисуй" });
                    mes = await SimpleRequest($"переведи запрос на английский язык, вот запрос: ({mes}), ответ предоставь одной строчкой, только полученный перевод запроса.");

                    string fName = imagePath.Replace(".jpg", "_repaint.jpg");
                    await StableDiffusion.StableDiffusionImgToImg(mes, imagePath, fName, 3);
                    try
                    {
                        if (System.IO.File.Exists(fName))
                        {
                            Message mM;
                            string ServerRelativePath = "../Imagine/" + System.IO.Path.GetFileName(Path.Combine(Paths.Imagine, fName));
                            using (Stream stream = System.IO.File.OpenRead(fName))
                            {
                                mM = await Bot.SendPhotoAsync(
                                chatId: message.Chat.Id,
                                photo: InputFile.FromStream(stream, "filename.jpg"), // Здесь необходимо указать имя файла
                                caption: $"@{message.From.Username}: [{ServerRelativePath}]\n{message.Text} *{stopwatch.Elapsed.TotalSeconds.ToString("00")}*",
                                disableNotification: true,
                                parseMode: ParseMode.Markdown
                            );
                            };
                        }
                        else
                        {
                            await Bot.SendTextMessageAsync(message.Chat.Id, $" *{stopwatch.Elapsed.TotalSeconds.ToString("00")}s*", parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                            Logger.AddLog($"File not found: {fName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }

                }
                else
                {



                    questionBytes = System.Text.Encoding.UTF8.GetBytes(ask);
                    compressedQuestionBytes = CompressGzip(questionBytes);
                    compressedQuestionBase64 = Convert.ToBase64String(compressedQuestionBytes);
                    string response = ConnectToOpenAI_App(imagePath, (compressedQuestionBase64), "");

                    stopwatch.Stop();
                    await Bot.SendTextMessageAsync(message.Chat.Id, response + $" *{stopwatch.Elapsed.TotalSeconds.ToString("00")}s* +{mes}", parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown); ; ;
                }
            }


        }



        if (message.Type == MessageType.Text)
        {




            if (message.Text.ToLower().Contains("скинь фотки"))
            {
                m.Add(await Bot.SendTextMessageAsync(message.Chat.Id, "Ищу фотки. Ожидайте.", parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown));
                m.AddRange(await SendPhotos(message));

            }
            else
            {
                m.Add(await ReceiveText(message, stopwatch, ask, withStory));
            }
        }



        if (message.Type == MessageType.Voice)
        {
            if (message.Chat.Type == ChatType.Private) m.Add(await Bot.SendTextMessageAsync(message.Chat.Id, "Распознаю вашу бессвязную речь, 60 сек.", parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown));

            string audioPath = await LoadAudio(message);

            string text = Audio.ConvertOggToWav(audioPath);

            byte[] questionBytes = System.Text.Encoding.UTF8.GetBytes(ask);
            byte[] compressedQuestionBytes = CompressGzip(questionBytes);
            string compressedQuestionBase64 = Convert.ToBase64String(compressedQuestionBytes);
            if (System.String.IsNullOrEmpty(text))
                await Bot.SendTextMessageAsync(message.Chat.Id, "Audio not recognized!", parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);


            if (message.Chat.Type == ChatType.Private) 
        { 
            string response = ConnectToOpenAI_App("", compressedQuestionBase64, "");
            try
            {
                await Bot.SendTextMessageAsync(message.Chat.Id, response + $"P{text} *{stopwatch.Elapsed.TotalSeconds.ToString("00")}s*", parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            } 
        }
        }
        // foreach (var item in m)
        try
        {
            await Bot.DeleteMessageAsync(m[0].Chat.Id, m[0].MessageId);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }





    }
    private static string Clear(string inp, string[] phrases)
    {
        foreach (string p in phrases)
        {
            if (inp.ToLower().Contains(p.ToLower()))
            {
                inp = inp.Replace(p, "");
            }
        }
        return inp;


    }
    private static async Task<Message[]> SendPhotos(Message message)
    {
        string[] albumToSend = GetPhotosFromAlbum(0, message, 10);
        var photoCount = albumToSend.Length;

        // Проверяем, есть ли фотографии для отправки
        if (photoCount == 0)
        {
            return null; // или возвращаем пустой массив, если это предпочтительнее
        }

        var media = new List<IAlbumInputMedia>();
        var streams = new List<Stream>(); // Список для хранения открытых потоков

        try
        {
            // Создаем потоки для каждой фотографии и добавляем их в список медиа
            foreach (var photoPath in albumToSend)
            {
                var stream = System.IO.File.OpenRead(photoPath);
                streams.Add(stream); // Добавляем поток в список, чтобы потом корректно закрыть

                media.Add(new InputMediaPhoto(InputFile.FromStream(streams.Last(), Path.GetFileName(photoPath))));
            }

            // Отправляем все фотографии как альбом
            var messages = await Bot.SendMediaGroupAsync(
                chatId: message.Chat.Id,
                media: media.ToArray(),
                cancellationToken: default
            );

            return messages;
        }
        finally
        {
            // Закрываем все потоки после отправки или при возникновении исключения
            foreach (var stream in streams)
            {
                stream.Close();
            }
        }
    }

    private static string[] GetPhotosFromAlbum(int days, Message m, int maxPhotosCount)
    {
        string chatPath = GetChatPath(m);
        // Получаем текущую дату и вычитаем заданное количество дней
        DateTime targetDate = DateTime.Now.Date.AddDays(-days);

        // Получаем все файлы .jpg из директории
        string[] files = Directory.GetFiles(chatPath, "*.jpg");

        // Фильтруем файлы по дате модификации
        var filteredFiles = files.Where(file =>
        {
            DateTime lastWriteTime = System.IO.File.GetLastWriteTime(file).Date;
            return lastWriteTime == targetDate;
        })
        .OrderByDescending(file => System.IO.File.GetLastWriteTime(file)) // Сортируем файлы по дате модификации в обратном порядке
        .Take(maxPhotosCount) // Берем только первые maxPhotosCount файлов
        .ToArray();

        return filteredFiles;
    }

    private static async Task<Message> ReceiveText(Message message, Stopwatch stopwatch, string ask, bool withHistory)
    {
        string his = $"{ask.Length}";

        Message m = await Bot.SendTextMessageAsync(message.Chat.Id, $"Думаю, 15 сек. История Чата({his} символов.)", parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);

        if (message.Text.Length <= 5)
        {
            await Bot.SendTextMessageAsync(message.Chat.Id, "Основные Команды\n бот история - ваш вопрос по истории чата\n нaрисуй -ваш промпт\n отправьте фото или аудио сообщение", parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
            return m;
        }

        if (message.Text.ToLower().Contains("нарисуй") && withHistory == true)
        {
            ask = "нарисуй " + await SimpleRequest("Создай промпт для Dalle-3 по указанному запросу " + message.Text);
        }




        try
        {
            byte[] questionBytes = System.Text.Encoding.UTF8.GetBytes(ask);
            byte[] compressedQuestionBytes = CompressGzip(questionBytes);
            string compressedQuestionBase64 = Convert.ToBase64String(compressedQuestionBytes);


            string response = ConnectToOpenAI_App("", compressedQuestionBase64, "");

            string fName = $"{DateTime.Now:yyyyMMdd_HHmmssfff}.png";
            string filePath = Path.Combine(Paths.Imagine, fName);
            if (response.Contains("https://oaidalleapiprodscus.blob.core.windows.net"))
            {
                await DownloadFileAsync(response.Replace(" <T>", ""), Path.Combine(Paths.Imagine, fName));
            }
            if (response.Length > 4000) response = response.Substring(0, 4000);

            //

            try
            {
                if (System.IO.File.Exists(Path.Combine(Paths.Imagine, fName)))
                {
                    Message mM;
                    string ServerRelativePath = "../Imagine/" + System.IO.Path.GetFileName(Path.Combine(Paths.Imagine, fName));
                    using (Stream stream = System.IO.File.OpenRead(filePath))
                    {
                        mM = await Bot.SendPhotoAsync(
                        chatId: message.Chat.Id,
                        photo: InputFile.FromStream(stream, "filename.jpg"), // Здесь необходимо указать имя файла
                        caption: $"@{message.From.Username}: [{ServerRelativePath}]\n{message.Text} *{stopwatch.Elapsed.TotalSeconds.ToString("00")}*",
                        disableNotification: true,
                        parseMode: ParseMode.Markdown
                    );
                    };
                }
                else
                {
                    await Bot.SendTextMessageAsync(message.Chat.Id, response + $" *{stopwatch.Elapsed.TotalSeconds.ToString("00")}s*", parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                    Logger.AddLog($"File not found: {filePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        return m;
    }

    private static async Task<string> SimpleRequest(string ask)
    {
        byte[] questionBytes = System.Text.Encoding.UTF8.GetBytes(ask);
        byte[] compressedQuestionBytes = CompressGzip(questionBytes);
        string compressedQuestionBase64 = Convert.ToBase64String(compressedQuestionBytes);


        return ConnectToOpenAI_App("", compressedQuestionBase64, "");
    }


    private static async Task<Message> GenerateSiteAnswerToTelegram(Message message, Stopwatch stopwatch, string ask, bool withHistory)
    {
        string his = $"{ask.Length}";

        Message m = await Bot.SendTextMessageAsync(message.Chat.Id, $"Трах тибидох! Тяп ляп и в продакшен! История Чата({his} символов.)", parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);

        if (message.Text.Length <= 5)
        {
            await Bot.SendTextMessageAsync(message.Chat.Id, "Основные Команды\n бот история - ваш вопрос по истории чата\n нaрисуй -ваш промпт\n отправьте фото или аудио сообщение", parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
            return m;
        }
        try
        {
            ask = ask.Replace("нарисуй", "");
            byte[] questionBytes = System.Text.Encoding.UTF8.GetBytes(ask);
            byte[] compressedQuestionBytes = CompressGzip(questionBytes);
            string compressedQuestionBase64 = Convert.ToBase64String(compressedQuestionBytes);

            string response = ConnectToOpenAI_App("", compressedQuestionBase64, "");

            if (response.StartsWith("```json")) response = response.Substring(7);
            if (response.EndsWith("```")) response = response.Substring(0, response.Length - 3);
            string SiteName = "";
            try
            {
                SiteName = ParseAndCreateFiles(response);
            }
            catch (Exception ex)
            {
                await Bot.SendTextMessageAsync(message.Chat.Id, $"Exception Parsing Site:{ex.Message}: " + response + $" *{stopwatch.Elapsed.TotalSeconds.ToString("00")}*", parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);

            }
            //if (response.Length > 4000) response = response.Substring(0, 4000);





            await Bot.SendTextMessageAsync(message.Chat.Id, SiteName + $" *{stopwatch.Elapsed.TotalSeconds.ToString("00")}s*", parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        return m;
    }

    public static async Task DownloadFileAsync(string fileUrl, string savePath)
    {
        using (HttpClient client = new HttpClient())
        {
            try
            {
                byte[] fileBytes = await client.GetByteArrayAsync(fileUrl);
                await System.IO.File.WriteAllBytesAsync(savePath, fileBytes);
                Logger.AddLog($"File downloaded and saved to {savePath}");
            }
            catch (Exception ex)
            {
                Logger.AddLog($"Exception during file download: {ex.Message}");
            }
        }
    }
    public static string ParseAndCreateFiles(string jsonString)
    {
        // Initialize the list of dynamic objects
        List<dynamic> jsonObjList = new List<dynamic>();

        try
        {
            // Attempt to deserialize the JSON string as an array
            if (jsonString.Trim().StartsWith("["))
            {
                jsonObjList = JsonConvert.DeserializeObject<List<dynamic>>(ExtractJson(jsonString));
            }
            else
            {
                // Deserialize as a single object and add it to the list
                dynamic singleJsonObj = JsonConvert.DeserializeObject<dynamic>(ExtractJson(jsonString));
                jsonObjList.Add(singleJsonObj);
            }
        }
        catch (JsonSerializationException ex)
        {
            Logger.AddLog($"JsonSerializationException: {ex.Message}");
            return string.Empty; // Return empty if deserialization fails
        }

        string result = string.Empty;
        // Iterate through each object in the list
        foreach (var jsonObj in jsonObjList)
        {
            // Assign values to variables
            string siteName = jsonObj.siteName;
            string html = jsonObj.html;
            string css = jsonObj.css;

            // Determine the site directory path
            string sitePath = Path.Combine(Paths.Sites, siteName);
            Directory.CreateDirectory(sitePath);

            // Create HTML file
            string htmlFilePath = Path.Combine(sitePath, $"{siteName}.html");
            System.IO.File.WriteAllText(htmlFilePath, html);

            // Create CSS file
            string cssFilePath = Path.Combine(sitePath, $"{siteName}.css");
            System.IO.File.WriteAllText(cssFilePath, css);
            result += "https://renderfin.com/sites/" + siteName + "/" + siteName + ".html\n";
        }

        // Return the result string
        return jsonObjList.Count > 0 ? result : string.Empty;
    }
    static internal async Task<string> LoadImage(Message message)
    {
        try
        {
            var fileId = message.Photo.Last().FileId;
            var fileInfo = await Bot.GetFileAsync(fileId);
            var filePath = fileInfo.FilePath;
            //string filePath = $"https://api.telegram.org/file/bot<{token}}>/<FilePath>.;
            string destinationFilePath = Path.Combine(GetChatPath(message), message.Photo[0].FileId + ".jpg");

            string dir = Path.GetDirectoryName(destinationFilePath);
            if (!Directory.Exists(dir)) { Directory.CreateDirectory(dir); }

            await using Stream fileStream = System.IO.File.Create(destinationFilePath);

            await Bot.DownloadFileAsync(
            filePath: filePath,
            destination: fileStream);
            Console.WriteLine(filePath.ToString());
            return destinationFilePath;
            //Bot.SendTextMessageAsync(message.Chat.Id, $"File Downloaded {destinationFilePath}");
        } catch (Exception ex){ Console.WriteLine(ex.Message); }
        return string.Empty;
    }

    static internal async Task<string> LoadAudio(Message message)
    {
        var fileId = message.Voice.FileId;
        var fileInfo = await Bot.GetFileAsync(fileId);
        var filePath = fileInfo.FilePath;
        //string filePath = $"https://api.telegram.org/file/bot<{token}}>/<FilePath>.;
        string destinationFilePath = Path.Combine(GetChatPath(message), message.Voice.FileId + ".ogg");
        await using Stream fileStream = System.IO.File.Create(destinationFilePath);
        await Bot.DownloadFileAsync(
        filePath: filePath,
        destination: fileStream);
        Console.WriteLine(destinationFilePath.ToString());
        return destinationFilePath;
        //Bot.SendTextMessageAsync(message.Chat.Id, $"File Downloaded {destinationFilePath}");

    }
    static internal string GetNiCkNameFromMessage(Message message)
    {
        string nickName = message.Chat.Username;
        if (nickName == null)
        {
            nickName = message.Chat.FirstName + " " + message.Chat.LastName;
        }
        return nickName;
    }
    static internal string GetChatPath(Message message)
    {
        string group = "";
        if (message.Chat.Type == ChatType.Group)
            group = message.Chat.Title;
        string nickName = GetNiCkNameFromMessage(message);
        long chatId = message.Chat.Id;
        string chatPath = Path.Combine(Paths.Chats, group + $"{nickName}_{chatId}");
        return chatPath;
    }
    static internal void RegisterChat(Message message)
    {
        string nickName = GetNiCkNameFromMessage(message);
        long chatId = message.Chat.Id;
        string chatPath = Path.Combine(Paths.Chats, $"{nickName}_{chatId}");
        if (!chatIds.Contains(chatId))
        {
            chatIds.Add(chatId);
            System.IO.Directory.CreateDirectory(chatPath);
            Bot.SendTextMessageAsync(223960353, $"Hello @{message.From.Username}{message.From.FirstName}");
        }

    }
    static internal void LoadChats()
    {
        try
        {// получаем список чатов из директории Paths.Chats по принципу {nickName}_{chatID}.txt
            string[] files = Directory.GetDirectories(Paths.Chats);
            foreach (string file in files)
            {
                string[] parts = Path.GetFileNameWithoutExtension(file).Split('_');
                if (parts.Length == 2)
                {
                    long chatId = long.Parse(parts[1]);
                    chatIds.Add(chatId);
                }
            }
        }
        catch (Exception ex) { Console.WriteLine("Exception: " + ex.Message); }
    }

    static internal void TelegramBot(string token)
    {
        Bot = new TelegramBotClient(token);
        Bot.StartReceiving(TelegramUpdate, Error);

    }
    private static async Task Error(ITelegramBotClient client, Exception exception, CancellationToken token)
    {
        Console.WriteLine($"Error: {exception.Message}");
    }

    public static string MES = "[MES]";
    public static string _MES = MES.Insert(1, "_");
    public static string TXT = "[TXT]";
    public static string _TXT = TXT.Insert(1, "_");
    public static string B64_TXT = "[B64-TXT]";
    public static string _B64_TXT = B64_TXT.Insert(1, "_");
    public static string B64_IMG = "[B64-IMG]";
    public static string _B64_IMG = B64_IMG.Insert(1, "_");
    public static string IMG_PATH = "[IMG_PATH]";
    public static string _IMG_PATH = IMG_PATH.Insert(1, "_");
    public static string ConnectToOpenAI_App(string imagePath, string b64text, string output)
    {
        ProcessStartInfo start = new ProcessStartInfo();
        start.FileName = Paths.ConsoleApp;
        string args = "";
        if (string.IsNullOrEmpty(imagePath))
        {
            args = $"{MES}{B64_TXT}{b64text}{_B64_TXT}{_MES}";
        }
        else
            args = $"{MES}{B64_TXT}{b64text}{_B64_TXT}{IMG_PATH}{Path.GetFullPath(imagePath)}{_IMG_PATH}{_MES}";
        start.Arguments = args;// -output \"{output}\""; // python script_path image_path text_query
        start.Arguments = start.Arguments.Trim(' ');
        start.UseShellExecute = false;
        start.RedirectStandardOutput = true; // read output
        start.RedirectStandardError = true; // read error
        start.CreateNoWindow = true; // no window



        string result = "";

        using (Process process = Process.Start(start))
        {
            using (StreamReader reader = process.StandardOutput)
            {
                result = reader.ReadToEnd(); // read output
            }

            using (StreamReader reader = process.StandardError)
            {
                string error = reader.ReadToEnd(); // read error
                if (!string.IsNullOrEmpty(error))
                {

                }
            }
        }
        //UnityEngine.Debug.Log(result);
        string tokens = "";
        try
        {
            tokens = Regex.Match(result, @"\[Tokens\](.*?)\[Tokens\]", RegexOptions.Singleline).Groups[1].Value.Trim();
        }
        catch { }

        result = Regex.Match(result, @"\[Response\](.*?)\[Response\]", RegexOptions.Singleline).Groups[1].Value.Trim();

        result = result + " <" + tokens + "T>";

        return result; // return output
    }
    public static byte[] CompressGzip(byte[] data)
    {
        using (var compressedStream = new MemoryStream())
        {
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                zipStream.Write(data, 0, data.Length);
            }
            return compressedStream.ToArray();
        }
    }
    public static string ExtractJson(string text)
    {
        int startIndex = -1;
        int endIndex = -1;
        int openBraces = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '{')
            {
                if (openBraces == 0)
                {
                    startIndex = i;
                }
                openBraces++;
            }
            else if (text[i] == '}')
            {
                openBraces--;
                if (openBraces == 0)
                {
                    endIndex = i;
                    break;
                }
            }
        }

        if (startIndex != -1 && endIndex != -1)
        {
            return text.Substring(startIndex, endIndex - startIndex + 1);
        }

        return string.Empty;
    }

    public static async Task<Message> SendTextMessage(long chatID, string message)
    {
        Task.Delay(100);
        Message m = null;
        try
        {
            m = await Bot.SendTextMessageAsync(chatID, Phrases.Narisuy[new Random().Next(Phrases.Narisuy.Length)], parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,disableNotification: true);
        }
        catch (Exception e)
        {

            Console.WriteLine(e);
        }
        return m;

    }
    public static async Task<Message> EditTextMessage(Message lastMessage, string newMessage)
    {
        await Task.Delay(50);
        Message m = null;
        try
        {
            m = await Bot.EditMessageTextAsync(
                lastMessage.Chat.Id,
                lastMessage.MessageId,
                newMessage,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown
                

            );
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return m;
    }
    public static async Task<Message> SendPhotoMessage(long ChatID, string filePath, string Caption,string username,string prompt)
    {
        Message mM=null;
        try
        {
            string ServerRelativePath = "../Imagine/" + System.IO.Path.GetFileName(filePath);
            var inlineKeyboard = CreateInlineKeyboard("Повторить!", ServerRelativePath);

           
            
            using (Stream stream = System.IO.File.OpenRead(filePath))
            {
                mM = await Bot.SendPhotoAsync(
                chatId: ChatID,
                photo: InputFile.FromStream(stream, "filename.jpg"), // Здесь необходимо указать имя файла
                caption: $"{username} {Caption}\n{prompt}",
                disableNotification: true,
                parseMode: ParseMode.Markdown,
                replyMarkup: inlineKeyboard
                
            );
            };
        }
        catch (Exception ex) { Console.WriteLine(ex.Message); };
        return mM;
    }
    public static async Task<bool> DeleteTextMessage(Message lastMessage)
    {
        await Task.Delay(50);
        bool result = false;
        try
        {
            await Bot.DeleteMessageAsync(lastMessage.Chat.Id, lastMessage.MessageId);
            result = true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return result;
    }
    public static InlineKeyboardMarkup CreateInlineKeyboard(string buttonText, string callbackData)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(buttonText, callbackData),
                InlineKeyboardButton.WithCallbackData("Real", "1"),
                InlineKeyboardButton.WithCallbackData("Toon", "2"),
                InlineKeyboardButton.WithCallbackData("Pron", "3"),
                InlineKeyboardButton.WithCallbackData("Pron2", "4"),
            }
        });
    }
}
