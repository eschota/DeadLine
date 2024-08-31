using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class Options
{
    public static Webserver webserver = new Webserver();
    public static TGBot tgbot = new TGBot();
    public static string RootPassword = "Zaebaliuzhe33";
    #region WebServer
    public class Webserver
    {
        public static string PfxFilePath;
        public static string PfxPassword;
        public static int Port;
    } 
    #endregion
    #region TGBot
    public class TGBot
    {
        public static string Token;
        public static double[] SubscribedChatIDs;
    }
    #endregion

    #region SaveLoad
    public static void SaveToJson(string filePath)
    {
        string json = JsonConvert.SerializeObject(new { webserver, tgbot, RootPassword });
        File.WriteAllText(filePath, json);
    }

    public static void LoadFromJson(string filePath)
    {
        string json = File.ReadAllText(filePath);
        dynamic data = JsonConvert.DeserializeObject<dynamic>(json);
        webserver = JsonConvert.DeserializeObject<Webserver>(data.webserver.ToString());
        tgbot = JsonConvert.DeserializeObject<TGBot>(data.tgbot.ToString());
        RootPassword = data.RootPassword;
    }
    #endregion

    
}
#region OptionsModel 
public class OptionsModel : PageModel
{
    [BindProperty]
    public Options.Webserver WebserverOptions { get; set; }

    [BindProperty]
    public Options.TGBot TGBotOptions { get; set; }

    public void OnGet()
    {
        WebserverOptions = Options.webserver;
        TGBotOptions = Options.tgbot;
    }

    public void OnPost()
    {
        Options.webserver = WebserverOptions;
        Options.tgbot = TGBotOptions;
        Options.SaveToJson("path_to_json_file");
    }
}

#endregion


