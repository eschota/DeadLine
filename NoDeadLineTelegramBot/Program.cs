using static Paths;
using static Chat;
using System.Runtime;
using System.Text;
using System.Reflection;
using Python.Runtime;
using System; 
internal class Program
{
    internal static async Task Main(string[] args)
    {
        await ComfyUI_adapter.TestRun("Hello beautiful girl");

        Initialize(args);
        LoadChats();
        //Games.LoadAllGames();
        await SDAdapter.RestartStableDiffusion();

        TelegramBot("7198960639:AAFTzz2ZcwggOErqLQpTUtywI__xO5BMsqM");
       // await new VoskSpeechRecognizer("model").StartSpeechRecognitionAsync();



        Console.WriteLine("Hello, World!"); 
        while (Console.ReadLine().ToLower() != "q") ; 
    }
   


}