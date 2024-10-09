using static Paths;
using static Chat;
using System.Runtime;
using System.Text;
using System.Reflection;
using Python.Runtime;
using System;
using NoDeadLineTelegramBot;
internal class Program
{
    internal static async Task Main(string[] args)
    {
        var history = MessageHistory.Instance;
        history.LoadAllMessages(Paths.Chats);

        

        Initialize(args);
        
        //Games.LoadAllGames();
      //  await SDAdapter.RestartStableDiffusion();

        TelegramBot("7198960639:AAFTzz2ZcwggOErqLQpTUtywI__xO5BMsqM");
        // await new VoskSpeechRecognizer("model").StartSpeechRecognitionAsync();

        var ftpUploader = new FtpUploader("ftp://ftp.microstock.plus", "eschota@gmail.com", "91clb6jqwd");
        ftpUploader.UploadFile("c:\\ComfyUI\\web\\zz_.png", "zz_.png");

        Console.WriteLine("Hello, World!"); 
        while (Console.ReadLine().ToLower() != "q") ;
    }
   


}