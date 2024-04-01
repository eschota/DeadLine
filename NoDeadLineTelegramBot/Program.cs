using static Paths;
using static Chat;
using System.Runtime;

internal class Program
{    
    internal static void Main(string[] args)
    { 
        Initialize(args);
        LoadChats();
        TelegramBot(token);
         
        Console.WriteLine("Hello, World!");
        while (Console.ReadLine().ToLower() != "q");
    }
}