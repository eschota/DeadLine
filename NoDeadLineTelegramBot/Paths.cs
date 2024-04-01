using System.Security.Principal;

internal static class Paths
{
    internal static string token="";
    
    internal static string audio = "";

    internal static string []BaseArgs = {"7198960639:AAEz2yNiiSF-vvi1l0QYTezDbcUEMR3MpgY","C:\\OneClickUnityDefaultProjects\\OpenAIClient\\build\\OpenAIClient.exe",
    "C:\\NoDeadLineTelegramBot\\NoDeadLineTelegramBot\\whisper"};
    internal static string AppPath => AppDomain.CurrentDomain.BaseDirectory; 
    internal static string Chats => Path.Combine(AppPath, "Chats"); 
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
        if (!Directory.Exists(Logs))
        {
            Directory.CreateDirectory(Logs);
        }
    }
}