
using Microsoft.Extensions.Hosting;
using static System.Net.WebRequestMethods;

internal class Program
    {
    public static Site TS;
    public static Site Unity;
    public static Site UnityTools;
        static async Task Main(string[] args)
    {
        Paths.IniPaths();
        BD.LoadWorkers();
        TGBot.StartBot();
        WebServer.RunServerAsync(443);
   
        
        Unity = new Site("Unity3D", 500, "https://assetstore.unity.com/?category=3d&orderBy=1&page=0&rows=96");
        UnityTools = new Site("UnityTools", 100, "https://assetstore.unity.com/?category=tools&orderBy=1&page=0&rows=96");
        TS = new Site("TurboSquid", 2000);

        TSParse.ParseTopPages(Directory.GetDirectories(Program.TS.RawFolder));

        new Worker().DownloadPages();
        while (true) 
        { 
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
            break;
        }
        }
     
    } 
