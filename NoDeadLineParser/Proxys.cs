using OpenAIClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal class Proxys
{
    private static string EliteProxyList = "c:\\OneClickUnityDefaultProjects\\OpenAIClient\\proxy.list";

    public static List<Proxys> proxies = new List<Proxys>();
    public string login;
    public string password;
    public int port;
    public string host;
    public Proxys(string _host, string _port, string log="" ,string pass = "" )
    {
        login = log;
        password = pass;
        //try to parse port to int
        int.TryParse(_port, out port);

        host = _host;
    }
    public static async Task GetFreeProxy()
    {
        proxies.Clear();
        // нахуй GetProxyByFile("https://raw.githubusercontent.com/hookzof/socks5_list/master/proxy.txt");
        GetProxyByFile("https://raw.githubusercontent.com/proxy4parsing/proxy-list/main/http.txt");

        GetProxyByFile("https://raw.githubusercontent.com/jetkai/proxy-list/main/online-proxies/txt/proxies.txt");
        GetProxyByFile("https://raw.githubusercontent.com/TheSpeedX/SOCKS-List/master/socks4.txt");
        GetProxyByFile("https://raw.githubusercontent.com/TheSpeedX/SOCKS-List/master/http.txt");
        GetProxyByFile("https://raw.githubusercontent.com/TheSpeedX/SOCKS-List/master/socks5.txt");
        GetProxyByFile("https://raw.githubusercontent.com/MuRongPIG/Proxy-Master/main/http.txt");
   

        proxies = proxies.OrderBy(x => Guid.NewGuid()).ToList();
        GetProxyByFileWithAutorization();
    }

    private static void GetProxyByFileWithAutorization()
    {
        if(File.Exists(EliteProxyList))
        {
           string[] prox=  File.ReadAllLines(EliteProxyList);
            foreach (var item in prox)
            {
                proxies.Insert(0,new Proxys(item.Split(';')[0], item.Split(';')[1], item.Split(';')[2], item.Split(';')[3]));
            }
        }

        
    }
    
    public class Proxy
    {
        public string Ip { get; set; }
        public string Port { get; set; }
    }
    private static void GetProxy2()
    {
        WebClient client = new WebClient();
        try
        {
            string proxyListUrl = "https://proxylist.geonode.com/api/proxy-list?limit=500&page=1&sort_by=lastChecked&sort_type=desc";
            string json = client.DownloadString(proxyListUrl);

            // Десериализация JSON
            JObject jsonObj = JObject.Parse(json);
            JArray dataArray = (JArray)jsonObj["data"];

            foreach (var item in dataArray)
            {
                Proxy proxy = item.ToObject<Proxy>();
                proxies.Insert(0,new Proxys( proxy.Ip, proxy.Port));
            }



        }
        catch (WebException ex)
        {
            Console.WriteLine("Ошибка: " + ex.Message);
        }
    }
    private static void GetProxyByFile(string file)
    {
        WebClient client = new WebClient();
        try
        {
            string proxyListUrl = file;
            string json = client.DownloadString(proxyListUrl);

            foreach (var item in json.Split('\n'))
            {
                try
                {
                    if(!proxies.Any(p => p.host == item.Split(':')[0] && p.port.ToString() == item.Split(':')[1]))
                    proxies.Add(new Proxys(item.Split(':')[0], item.Split(':')[1]));
                }
                catch (Exception ex)
                { };
            }

            Console.WriteLine("\nProxy Found: " + proxies.Count+" : "+file);

        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка: " + ex.Message);
        }
    } 
}
