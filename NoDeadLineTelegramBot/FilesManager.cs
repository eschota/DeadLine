using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

public static class FilesManager
    {
    public static List<Create> creates = new List<Create>();
    private static string _create = " -create ";

    public static void LoadCreates()
    {
        var Files = Directory.GetFiles(Paths.CreatesDirectory);
        foreach (var item in Files)
        {
            creates.Add(JsonConvert.DeserializeObject<Create>(System.IO.File.ReadAllText(item)));
        }
    }

    public static List<Message> CreateHistory = new List<Message>();

    public static async Task<bool> IsCreate(Message message)
    {
        if (message.Text == null) return false;
        try
        
        {
            string text = message.Text.ToLower();

            if (text.Contains(_create))
            {

                // Извлечение пути из сообщения
                string path = text.Substring(text.IndexOf(_create) + _create.Length); // Используем Trim для удаления пробельных символов в начале и конце пути
                string name = text.Substring(0, text.IndexOf(_create));
                // Проверка существования директории
                if (Directory.Exists(path))
                {
                    Console.WriteLine("Директория уже существует.");
                    await Chat.Bot.SendTextMessageAsync(message.Chat.Id, "Директория доступна. проект сохранен, для общения по проекту, отвечайте на это сообщение комментарием.");
                    SaveCreateToHistory(new Create(message, name, path));
                    return true;
                }
                else
                {
                    // Попытка создать директорию
                    await CreateFolder(message, path);
                    SaveCreateToHistory(new Create(message, name, path));
                    return true;
                }
            }
            Create current = null;
            if (message.ReplyToMessage != null)
            {
                foreach (var c in creates)
                {
                    for (int i = 0; i < c.versions.Count; i++)
                    {
                        if (c.versions[i].message.MessageId == message.ReplyToMessage.MessageId)
                        {
                            text = "$* " + text;
                            await GenerateAnswer(text, c, message.ReplyToMessage);
                            await Chat.Bot.SendTextMessageAsync(message.ReplyToMessage.Chat.Id, $"{c.name} : [{c.versions.Count}] - {ConvertPathToServerUrl(c.directory, "index.html")}");
                            return true;
                        }
                    }
                }
                foreach (var c in creates)
                {
                    if(message.ReplyToMessage.Text!=null)
                    if (message.ReplyToMessage.Text.StartsWith(c.name))
                    {
                        current = c;
                            text = "$* " + text;
                            await GenerateAnswer(text, c, message.ReplyToMessage);
                        await Chat.Bot.SendTextMessageAsync(message.ReplyToMessage.Chat.Id, $"{c.name} : [{c.versions.Count}] - {ConvertPathToServerUrl(c.directory,"index.html")}");
                        return true;
                    }
                }
            }
           
            foreach (var c in creates)
            {
                if (text.StartsWith(c.name))
                {
                    current = c; 

                    await GenerateAnswer(text,c, message);
                    await Chat.Bot.SendTextMessageAsync(message.Chat.Id, $"{c.name} : [{c.versions.Count}] - {ConvertPathToServerUrl(c.directory,"index.html")}");
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            return true;
        }
    }
    public static string ConvertPathToServerUrl(string path,string pattern)
    {
        string res = "";
        if (path.Contains("wwwroot"))
        {
            res = path.Substring(path.IndexOf("wwwroot"));
            res = res.Replace('\\', '/');
            string index = "";
            index = Directory.GetFiles(path, pattern)[0];
            if (index != "")
            {
                if (res[res.Length-1]!='/')
                res += $"/{pattern}";
            }


            return res.Replace("wwwroot", "https://renderfin.com");
        }
        return path;
    }
   

    public static async Task GenerateAnswer(string ask,Create c,Message m)
   {
    try{
        ask = await ConvertPrompt(ask,c);
        ask += " " + "Всегда возвращай полное содержимое всех файлов проекта, даже если в них нет никаких изменений. Все файлы работают как единое целое и не должны иметь ошибок, особенно в импорте функций друг друга.";

        string response = await OpenAIClient.AskOpenAI_formatted_response(ask, c,m.Chat.Id);
        c.versions.Add(new Version(m,response));
        SaveCreateToHistory(c);
        var files = Directory.GetFiles(c.directory);
        foreach (var item in files)
        {
            if (item.Contains("response"))
            {
                System.IO.File.Delete(item);
            }
        }

        string answer = "";
        var responseObject = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response);
        foreach (var item in responseObject.files)
        {
            string fileName = item.file_name;
            string fileContent = item.file_content;

            answer += fileName+" ";
            System.IO.File.WriteAllText(Path.Combine(c.directory, fileName), fileContent);
        }
        }catch ( Exception ex)
        {
            await Chat.Bot.SendTextMessageAsync(m.Chat.Id, ex.Message);
        }
    }

    public static async Task<string> ConvertPrompt(string ask, Create c)
    {
        // Регулярное выражение для поиска подстрок, начинающихся на $ и заканчивающихся пробелом
        string pattern = @"\$([^\s]+)";
        try
        {
            // Находим все совпадения в строке
            MatchCollection matches = Regex.Matches(ask, pattern);
            foreach (Match match in matches)
            {
                // Используем значение из первой группы, которое представляет собой строку после $
                string searchPattern = match.Groups[1].Value + "*";
                // Получаем список файлов, соответствующих шаблону поиска
                var files = Directory.GetFiles(c.directory, searchPattern);
                foreach (var file in files)
                {
                    string content = "";
                    if (file.ToLower().EndsWith(".jpg") || file.ToLower().EndsWith(".png"))
                    {
                        
                        ask += $"\n используй это\n image: {ConvertPathToServerUrl(c.directory,Path.GetFileName(file))} ";
                    }
                    else
                    {
                        content = await System.IO.File.ReadAllTextAsync(file);
                        ask += $"\n используй это\n Filename: {Path.GetFileName(file)}, content: {content} "; 
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await Chat.Bot.SendTextMessageAsync(c.versions.Last().message.Chat.Id, ex.Message);
            return ask; // Возвращаем измененный ask даже в случае исключения
        }
        return ask;
    }
    public static async Task<bool> CreateFolder(Message m,string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            await Chat.Bot.SendTextMessageAsync(m.Chat.Id, "Директория Создана. проект сохранен, для общения по проекту, отвечайте на это сообщение комментарием.");
            return true;
        }
        catch (Exception e)
        {
            await Chat.Bot.SendTextMessageAsync(m.Chat.Id, "Ошибка при создании директории. " +e.Message);
            Console.WriteLine($"Ошибка при создании директории: {e.Message}");
            return true;
        }
    }

    public static void SaveCreateToHistory(Create c) 
    { 
        if(!creates.Exists(X=>X.name==c.name)) creates.Add(c);
        System.IO.File.WriteAllText(Path.Combine(Paths.CreatesDirectory, $"{c.name}.json"), JsonConvert.SerializeObject(c));
    }
    


     public class Create
    {
        public Create (Message m, string _name,string path)
        {
            
            name = _name;
            directory = path;
            versions.Add(new Version(m,""));
            UUID = Guid.NewGuid().ToString();
            
        }
        public List<Version> versions = new List<Version>();
        public string UUID { get; set; }
        public string name;
        public string directory;
        public string ai_answer;  
        
    }
    public class Version
    { 
        public Message message;
        public string answer;
        public Version(Message m,string a)
        { 
            message = m;
            answer = a;
        }
    }
}
