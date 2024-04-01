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

                if (update.Type == UpdateType.Message)
                {                     
                     UpdateChatID(update.Message);
                }

            }
            catch { }
        }
        // если чат не найден, то добавляем его в список чатов
        static internal async void UpdateChatID(Message message)
        {
        var stopwatch = Stopwatch.StartNew();
        if (!chatIds.Contains(message.Chat.Id))
            {
                RegisterChat(message);
            }
        List<Message> m = new List<Message>();
         
            if (message.Type == MessageType.Photo)
            {
             m.Add( await Bot.SendTextMessageAsync(message.Chat.Id, "Распознаю фото, 30 сек.", parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown));
            string imagePath = await LoadImage(message);
                byte[] questionBytes = System.Text.Encoding.UTF8.GetBytes("Что на этой картинке?");
                if (!String.IsNullOrEmpty(message.Caption))
                questionBytes = System.Text.Encoding.UTF8.GetBytes(message.Caption);
                string response = ConnectToOpenAI_App(imagePath, System.Convert.ToBase64String(questionBytes), "");

                stopwatch.Stop();
            await Bot.SendTextMessageAsync(message.Chat.Id, response +$" *{stopwatch.Elapsed.TotalSeconds.ToString("00")}*" , parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown); ; ;
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
                m.Add(await ReceiveText(message, stopwatch));
            }
        } 
            
    

        if (message.Type == MessageType.Voice)
            {
            m.Add(await Bot.SendTextMessageAsync(message.Chat.Id, "Распознаю вашу бессвязную речь, 60 сек.", parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown));

            string audioPath = await LoadAudio(message);

                string text = Audio.ConvertOggToWav(audioPath);

                byte[] questionBytes = System.Text.Encoding.UTF8.GetBytes(text);
                if (String.IsNullOrEmpty(text))
                await Bot.SendTextMessageAsync(message.Chat.Id, "Audio not recognized!", parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);



                string response = ConnectToOpenAI_App("", System.Convert.ToBase64String(questionBytes), "");

                await Bot.SendTextMessageAsync(message.Chat.Id, response + $" *{stopwatch.Elapsed.TotalSeconds.ToString("00")}*", parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
            }
       // foreach (var item in m)
        {
            await Bot.DeleteMessageAsync(m[0].Chat.Id, m[0].MessageId);
        }
        




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

                media.Add(new InputMediaPhoto(InputFile.FromStream( streams.Last(), Path.GetFileName(photoPath))));
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

    private static async Task<Message> ReceiveText(Message message, Stopwatch stopwatch)
    {
        Message m = await Bot.SendTextMessageAsync(message.Chat.Id, "Думаю, 15 сек.", parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);

        byte[] questionBytes = System.Text.Encoding.UTF8.GetBytes(message.Text);
        string response = ConnectToOpenAI_App("", System.Convert.ToBase64String(questionBytes), "");

        await Bot.SendTextMessageAsync(message.Chat.Id, response + $" *{stopwatch.Elapsed.TotalSeconds.ToString("00")}*", parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
        return m;
    }

    static internal async Task <string> LoadImage(Message message)
        {  
            var fileId = message.Photo.Last().FileId;
            var fileInfo = await Bot.GetFileAsync(fileId);
            var filePath = fileInfo.FilePath;  
            //string filePath = $"https://api.telegram.org/file/bot<{token}}>/<FilePath>.;
            string destinationFilePath = Path.Combine(GetChatPath(message),message.Photo[0].FileId+".jpg");
            await using Stream fileStream = System.IO.File.Create(destinationFilePath);
            await Bot.DownloadFileAsync(
            filePath: filePath,
            destination: fileStream);
            Console.WriteLine(filePath.ToString());
            return destinationFilePath;
            //Bot.SendTextMessageAsync(message.Chat.Id, $"File Downloaded {destinationFilePath}");
            
        }
    
        static internal async Task <string> LoadAudio(Message message)
        {  
            var fileId = message.Voice.FileId;
            var fileInfo = await Bot.GetFileAsync(fileId);
            var filePath = fileInfo.FilePath;  
            //string filePath = $"https://api.telegram.org/file/bot<{token}}>/<FilePath>.;
            string destinationFilePath = Path.Combine(GetChatPath(message),message.Voice.FileId+".ogg");
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
            string nickName = GetNiCkNameFromMessage(message);
            long chatId = message.Chat.Id;
            string chatPath = Path.Combine(Paths.Chats, $"{nickName}_{chatId}");
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
                Bot.SendTextMessageAsync(message.Chat.Id, $"Hello {nickName}");
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
        }else
        args = $"{MES}{B64_TXT}{b64text}{_B64_TXT}{IMG_PATH}{Path.GetFullPath(imagePath)}{_IMG_PATH}{_MES}";
    start.Arguments =  args;// -output \"{output}\""; // python script_path image_path text_query
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

        result = Regex.Match(result, @"\[Response\](.*?)\[Response\]", RegexOptions.Singleline).Groups[1].Value.Trim();



        return result; // return output
}
}