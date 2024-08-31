using System.Security.Principal;

internal static class Paths
{
    internal static string token="";
    
    internal static string audio = "";

    internal static string []BaseArgs = {"7198960639:AAEz2yNiiSF-vvi1l0QYTezDbcUEMR3MpgY","C:\\OneClickUnityDefaultProjects\\OpenAIClient\\build\\OpenAIClient.exe",
    "C:\\NoDeadLineTelegramBot\\NoDeadLineTelegramBot\\whisper"};
    internal static string AppPath => AppDomain.CurrentDomain.BaseDirectory; 
    internal static string Chats => Path.Combine(AppPath, "Chats");
    internal static string Games => Path.Combine(AppPath, "Games");
    internal static string CreatesDirectory => Path.Combine(AppPath, "Creates");
    internal static string GamesPromts => Path.Combine(AppPath, "Games","Prompts");
    internal static string Sites => "c:\\DeadLine\\DeadLine\\NoDeadLineParser\\bin\\Debug\\net8.0\\wwwroot\\Sites\\";
    internal static string Imagine => "c:\\DeadLine\\DeadLine\\NoDeadLineParser\\bin\\Debug\\net8.0\\wwwroot\\Sites\\Imagine";

    internal static string ConsoleApp="";    
    internal static string Logs => Path.Combine(AppPath, "Logs");

    internal static string whisperModel => Directory.GetFiles(audio, "*.bin")[0];
    internal static string ffmpeg => Directory.GetFiles(audio, "*ffmpeg")[0];
    internal static string whisper => Path.Combine(audio, "whisper-faster-xxl.exe");

    // Инициализация необходимых директорий при старте приложения
    internal static void Initialize(string [] args)
    {
        if(args.Length==0)
        {
            ConsoleApp=BaseArgs[1];
            token = BaseArgs[0];             
            audio = BaseArgs[2];
        }
        // Создаем папку Chats, если она не существует
        if (!Directory.Exists(Chats))
        {
            Directory.CreateDirectory(Chats);
        } 
        if (!Directory.Exists(Games))
        {
            Directory.CreateDirectory(Games);
        } if (!Directory.Exists(GamesPromts))
        {
            Directory.CreateDirectory(GamesPromts);
        } 
        if (!Directory.Exists(Logs))
        {
            Directory.CreateDirectory(Logs);
        }    
        if (!Directory.Exists(CreatesDirectory))
        {
            Directory.CreateDirectory(CreatesDirectory);
        }
        FilesManager.LoadCreates();

    }
    public static void LimitFileCountInDirectory(string directoryPath, int maxFiles = 500)
    {
        if (!Directory.Exists(directoryPath))
        {
            Console.WriteLine($"Directory does not exist: {directoryPath}");
            return;
        }

        // Get all files in the directory
        var files = Directory.GetFiles(directoryPath);

        Console.WriteLine($"Found {files.Length} files in the directory.");

        // Check if the number of files exceeds the maximum allowed
        if (files.Length > maxFiles)
        {
            // Order files by creation time (oldest first)
            var filesToDelete = files.Select(file => new FileInfo(file))
                                     .OrderBy(fileInfo => fileInfo.CreationTime)
                                     .Take(files.Length - maxFiles);

            foreach (FileInfo fileInfo in filesToDelete)
            {
                try
                {
                    // Delete the file
                    fileInfo.Delete();
                    Console.WriteLine($"Deleted old file: {fileInfo.FullName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to delete file: {fileInfo.FullName}. Error: {ex.Message}");
                }
            }
        }
        else
        {
            Console.WriteLine("No files need to be deleted.");
        }
    }
}