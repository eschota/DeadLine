
using Microsoft.Extensions.Hosting;

internal class Program
    {
    public static Site TS;
    public static Site Unity;
    public static Site UnityTools;
        static void Main(string[] args)
        {
        WebServer.RunServerAsync(1478);
        Paths.IniPaths(); 
        TS = new Site("TurboSquid", 2000);
        Unity = new Site("Unity3D", 500, "https://assetstore.unity.com/?category=3d&orderBy=1&page=0&rows=96");
        UnityTools = new Site("UnityTools", 100, "https://assetstore.unity.com/?category=tools&orderBy=1&page=0&rows=96");
         
        
        new Worker().DownloadPages();
        while (true) 
        { 
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
            break;
        }
        }
     
    } 
