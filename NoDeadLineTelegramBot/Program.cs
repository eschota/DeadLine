using static Paths;
using static Chat;
using System.Runtime;
using System.Text;
using System.Reflection;
using Python.Runtime;

internal class Program
{
    internal static void Main(string[] args)
    {
        Initialize(args);
        LoadChats();
        TelegramBot(token);

        Console.WriteLine("Hello, World!");

        //try
        //{
        //    Runtime.PythonDLL = @"c:\Users\escho\AppData\Local\Programs\Python\Python310\python310.dll";
        //    Translator.InitializePythonEngine();
        //    string text = "Привет, как дела?";
        //    string translatedText = Translator.Translate(text);
        //    Console.WriteLine("Перевод: " + translatedText);
        //}catch (Exception ex) { Console.WriteLine(ex.ToString()); }

        // StableDiffusion.StableDiffusionImgToImg("beautiful girl", "c:\\CLI\\1.jpg", "c:\\CLI\\output_imageSD.png", 0);









        while (Console.ReadLine().ToLower() != "q") ;

       
    }
   


}