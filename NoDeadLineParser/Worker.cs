using OpenAIClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
 
    internal class Worker
{
    public class Page
    {
        public int tryCount = 0;
        public enum pageType {None, Top, Product}
        public pageType Type = pageType.None;
        public string url;
        public string filePage;
        public Site site;
        public Page (string _url, Site _site,string _file, pageType _type)
        {
            url = _url;
            site = _site;
            filePage = _file;
            Type = _type;
        }
    }
    public static List<Site> sites = new List<Site> ();
    private void FindAllPagesFromAllSites()
    {
        foreach (var item in sites)
        {
            item.FindPagesToParse();
        }

    }


    public static List<Page> pagesToParse = new List<Page>();
    static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(10000,10000);
    static Stopwatch byTen = new Stopwatch();static int currentPagesDownload = 0;
    private CancellationTokenSource _cancellationTokenSource;
    public async Task DownloadPages()
    {
        while (true) // Infinite loop to continuously check for messages
        {
            FindAllPagesFromAllSites();
            TSParse.ParseTopPages(Program.TS.RawFoldersAll); 

            Stopwatch Total = Stopwatch.StartNew();  byTen.Start(); currentPagesDownload = 0;
            while (pagesToParse.Count > 0)
            {
                await Proxys.GetFreeProxy();
                if (Total.Elapsed.Hours > 1) { pagesToParse.Clear(); if (_cancellationTokenSource != null) _cancellationTokenSource.Cancel(); }
                Console.WriteLine($"\nProxys: {Proxys.proxies.Count} Pages: {pagesToParse.Count}");

                Stopwatch sw = Stopwatch.StartNew();int cur = pagesToParse.Count;
                _cancellationTokenSource = new CancellationTokenSource();
                Task[] tasks = new Task[Proxys.proxies.Count]; 
                for (int i = 0; i < tasks.Length; i++)
                {
                    if (pagesToParse.Count <= 0) break;
                    await Task.Delay(new Random().Next(2, 10));
                    Page page = pagesToParse[i % pagesToParse.Count];
                    if(page==null)
                    {
                        pagesToParse.Remove(page);continue;
                    }
                    int taskNumber = i;
                    //if(i==tasks.Length-1) 
                    //{
                    //    ;
                    //}

                    //if (i % 1000 == 0 && i != 0) { Console.Write($"\n{Proxys.proxies.Count-i} Pages => {cur-pagesToParse.Count}/{pagesToParse.Count} [{sw.Elapsed} s.]\n");cur = pagesToParse.Count; sw.Restart(); }
                    tasks[i] = Task.Run(async () =>
                    {
                        await semaphoreSlim.WaitAsync();
                        try
                        {
                            await DownloadPage(i,page, Proxys.proxies[i % Proxys.proxies.Count]);
                        }
                        catch (Exception ex)
                            { Logger.AddLog("Exception Download Pages: " + ex.Message,Logger.LogLevel.Exceptions); }
                        finally
                        {
                            semaphoreSlim.Release();
                            
                        }
                    }, _cancellationTokenSource.Token);
                    //Console.WriteLine("Tasks Run!");
                }
                await Task.WhenAll(tasks);
               
            }
            
            if(Total.Elapsed.TotalSeconds>5) Console.WriteLine($"Total Time to Parse : {Total.Elapsed}");


            await Task.Delay(1000 * 60 * 60);
        }
    }
  
    static private async Task<bool> DownloadPage(int id,Page p, Proxys proxy, int repeat = 0)
    {
        var handler = new HttpClientHandler
        {
            Proxy = new System.Net.WebProxy($"http://{proxy.host}:{proxy.port}"),
            DefaultProxyCredentials = new System.Net.NetworkCredential(proxy.login, proxy.password)
        };


        string userAgent = GenerateUserAgent();

        using (HttpClient client = new HttpClient(handler)) // Использование handler
        {
           
            try
            {
                if (p == null) return false;
                //if (string.IsNullOrEmpty(proxy) || proxy.Split(':').Length != 2)
                //{
                //    Console.Write("*");
                //    return false;
                //}
                if(p.tryCount>10)
                {
                    int currentIndex = pagesToParse.IndexOf(p);
                    pagesToParse.RemoveAt(currentIndex);
                    Console.WriteLine("More then 10 try to download");
                    return false;
                }
                string safari = new Random().Next(470, 537).ToString()+"."+ new Random().Next(10, 36).ToString();
                client.DefaultRequestHeaders.Add("User-Agent", $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/{safari} (KHTML, like Gecko) Chrome/{new Random().Next(70,110)}.0.0.0 Safari/{safari}"); // Установка случайного User-Agent
                if (p == null) return false;
                var response = await client.GetAsync(p.url);
                response.EnsureSuccessStatusCode();
                string content = await response.Content.ReadAsStringAsync();


                if (content.Length > 2000)
                {
                    currentPagesDownload++;
                    if(currentPagesDownload % 100 == 0) 
                    { 
                        Console.WriteLine($"\n\nProds[{Program.TS.products.Count}] [{pagesToParse.Count}] FPS: [{(100/byTen.Elapsed.TotalSeconds ).ToString("0.0")}]  :: {DateTime.Now}\n");byTen.Restart();
                    }
                    if (!Directory.Exists(Path.GetDirectoryName(p.filePage))) Directory.CreateDirectory(Path.GetDirectoryName(p.filePage));
                    File.WriteAllText(p.filePage, content);
                    if(p.Type==Page.pageType.Product) TSParse.GetNewProductAfterParse(p.filePage);
                    //if(p.Type==Page.pageType.Top) TSParse.ParseTopPages(new string[] { Path.GetDirectoryName(p.filePage) });

                    int currentIndex = pagesToParse.IndexOf(p);
                    pagesToParse.RemoveAt(currentIndex); // Удаляем текущий обработанный элемент
                    if (proxy.login!="") Console.ForegroundColor= ConsoleColor.Green;
                    Console.Write($"[{repeat}]");
                    Console.ResetColor();
                    await Task.Delay(new Random().Next(500, 2500));

                    // Проверяем, что после текущего элемента в списке есть еще элементы
                    if (currentIndex < pagesToParse.Count)
                    {
                        // Вызываем DownloadPage для следующего элемента
                        DownloadPage(-1,pagesToParse[currentIndex], proxy, ++repeat);
                    }

                    return true;
                }
                else
                {
                    Logger.AddLog("Page Download Failed: Content too short.", Logger.LogLevel.Silent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                if(proxy.login!="") Console.ForegroundColor = ConsoleColor.Red;
                if (ex.Message.Contains("Forbidden"))
                {
                    if (repeat > 0)
                    {
                        Console.Write($"[F{repeat}]");
                        await Task.Delay(new Random().Next(1000, 2500));
                        DownloadPage(-1, pagesToParse[new Random().Next(0, pagesToParse.Count - 1)], Proxys.proxies[new Random().Next(0, Proxys.proxies.Count)], --repeat);
                    }
                    else
                    {
                        Console.Write("_");
                        await Task.Delay(new Random().Next(1000, 2500));
                        DownloadPage(-1, pagesToParse[new Random().Next(0, pagesToParse.Count - 1)], proxy, --repeat);
                    }
                }
                else
                {
                    if (repeat > 0)
                    {
                        Console.Write($"[E{repeat}]");
                        await Task.Delay(new Random().Next(1000, 2500));
                        DownloadPage(-1, pagesToParse[new Random().Next(0, pagesToParse.Count - 1)], proxy, --repeat);
                    }
                    else
                        Console.Write(".");
                }

                //else Console.Write(ex.Message.Substring(0,1));
                Console.ResetColor();

                return false;
            }
        }
    }
    private static Random random = new Random();

    // Генерация уникального User-Agent
    public static string GenerateUserAgent()
    {
        // Основные компоненты User-Agent строки
        string platform = GetRandomPlatform();
        string browser = GetRandomBrowser();
        string systemInfo = GetRandomSystemInfo();

        return $"{platform} {systemInfo} {browser}";
    }

    private static string GetRandomPlatform()
    {
        // Список платформ
        var platforms = new[]
        {
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)",
            "Mozilla/5.0 (X11; Linux x86_64)",
            "Mozilla/5.0 (Linux; Android 9; Pixel 3)"
        };

        return platforms[random.Next(platforms.Length)];
    }

    private static string GetRandomBrowser()
    {
        // Список браузеров с разными версиями
        var browsers = new[]
        {
            "AppleWebKit/537.36 (KHTML, like Gecko) Chrome/88.0.4324.150 Safari/537.36",
            "AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.0.3 Safari/605.1.15",
            "Gecko/20100101 Firefox/85.0",
            "AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.198 Mobile Safari/537.36"
        };

        return browsers[random.Next(browsers.Length)];
    }

    private static string GetRandomSystemInfo()
    {
        // Дополнительные системные данные
        var systemInfos = new[]
        {
            "rv:11.0",
            "",
            "en-US",
            "en-GB"
        };

        return systemInfos[random.Next(systemInfos.Length)];
    }
}

 