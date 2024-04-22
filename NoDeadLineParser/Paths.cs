using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

 
internal static  class Paths
    {

    public static string WorkerPath = "c:\\OneClickUnityDefaultProjects\\OpenAIClient\\build\\OpenAIClient.exe";
    public static string ParseFolder = Path.Combine(AppContext.BaseDirectory, "Parse");
    public static string NodeStatsDirectory = Path.Combine(AppContext.BaseDirectory, "Nodes");
    public static string [] Sites = { "https://www.turbosquid.com/Search/Index.cfm?page_num=1&size=200" };
    public static string LogFile => Path.Combine(AppContext.BaseDirectory, "ParserLog.log");
    public static void IniPaths()
    {//Create the Parse folder if it does not exist
        if (!Directory.Exists(ParseFolder))
        {
            Directory.CreateDirectory(ParseFolder);
        }
         if (!Directory.Exists(NodeStatsDirectory))
        {
            Directory.CreateDirectory(NodeStatsDirectory);
        }

    }
} 
