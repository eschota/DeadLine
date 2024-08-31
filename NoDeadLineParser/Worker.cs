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
 
    public class Worker
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
            //TSParse.ParseTopPages(Program.TS.RawFoldersAll); 

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


   static string[] users = {
    "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; Acoo Browser; .NET CLR 1.1.4322; .NET CLR 2.0.50727)",
    "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.0; Acoo Browser; SLCC1; .NET CLR 2.0.50727; Media Center PC 5.0; .NET CLR 3.0.04506)",
    "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; Acoo Browser; InfoPath.2; .NET CLR 2.0.50727; Alexa Toolbar)",
    "amaya/9.52 libwww/5.4.0",
    "amaya/11.1 libwww/5.4.0",
    "Amiga-AWeb/3.5.07 beta",
    "AmigaVoyager/3.4.4 (MorphOS/PPC native)",
    "AmigaVoyager/2.95 (compatible; MC680x0; AmigaOS)",
    "Mozilla/4.0 (compatible; MSIE 7.0; AOL 7.0; Windows NT 5.1; FunWebProducts)",
    "Mozilla/4.0 (compatible; MSIE 6.0; AOL 8.0; Windows NT 5.1; SV1)",
    "Mozilla/4.0 (compatible; MSIE 7.0; AOL 9.0; Windows NT 5.1; .NET CLR 1.1.4322; Zango 10.1.181.0)",
    "Mozilla/4.0 (compatible; MSIE 6.0; AOL 6.0; Windows NT 5.1)",
    "Mozilla/4.0 (compatible; MSIE 7.0; AOL 9.5; AOLBuild 4337.35; Windows NT 5.1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)",
    "Mozilla/5.0 (Windows; U; Windows NT 5.1; zh-CN) AppleWebKit/523.15 (KHTML, like Gecko, Safari/419.3) Arora/0.3 (Change: 287 c9dfb30)",
    "Mozilla/5.0 (X11; U; Linux; en-US) AppleWebKit/527+ (KHTML, like Gecko, Safari/419.3) Arora/0.6",
    "Mozilla/5.0 (X11; U; Linux; C -) AppleWebKit/523.15 (KHTML, like Gecko, Safari/419.3) Arora/0.5",
    "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; JyxoToolbar1.0;  Embedded Web Browser from: http://bsalsa.com/; Avant Browser; .NET CLR 2.0.50727; .NET CLR 3.0.04506.648; .NET CLR 3.5.21022; .NET CLR 1.1.4322)",
    "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; GTB5; Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1) ; Avant Browser)",
    "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; Avant Browser; Avant Browser; .NET CLR 1.1.4322; .NET CLR 2.0.50727; InfoPath.1)",
    "Mozilla/5.0 (X11; 78; CentOS; US-en) AppleWebKit/527+ (KHTML, like Gecko) Bolt/0.862 Version/3.0 Safari/523.15",
    "Mozilla/5.0 (Macintosh; U; PPC Mac OS X Mach-O; en-US; rv:1.7.2) Gecko/20040825 Camino/0.8.1",
    "Mozilla/5.0 (Macintosh; U; Intel Mac OS X Mach-O; en; rv:1.8.1.12) Gecko/20080206 Camino/1.5.5",
    "Mozilla/5.0 (Macintosh; U; PPC Mac OS X Mach-O; en-US; rv:1.0.1) Gecko/20030111 Chimera/0.6",
    "Mozilla/5.0 (Macintosh; U; PPC Mac OS X Mach-O; en-US; rv:1.8.0.10) Gecko/20070228 Camino/1.0.4",
    "Mozilla/5.0 (Macintosh; U; PPC Mac OS X Mach-O; en; rv:1.8.1.4pre) Gecko/20070511 Camino/1.6pre",
    "Mozilla/5.0 (Macintosh; U; PPC Mac OS X; en) AppleWebKit/418.9 (KHTML, like Gecko, Safari) Safari/419.3 Cheshire/1.0.ALPHA",
    "Mozilla/5.0 (Macintosh; U; PPC Mac OS X; en) AppleWebKit/419 (KHTML, like Gecko, Safari/419.3) Cheshire/1.0.ALPHA",
    "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US) AppleWebKit/525.19 (KHTML, like Gecko) Chrome/1.0.154.36 Safari/525.19",
    "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US) AppleWebKit/525.19 (KHTML, like Gecko) Chrome/1.0.154.53 Safari/525.19",
    "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.9.0.10) Gecko/2009042815 Firefox/3.0.10 CometBird/3.0.10",
    "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.0.5) Gecko/2009011615 Firefox/3.0.5 CometBird/3.0.5",
    "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; .NET CLR 1.1.4322; .NET CLR 2.0.50727; Crazy Browser 3.0.0 Beta2)",
    "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 1.1.4322; Crazy Browser 2.0.1)",
    "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; Crazy Browser 1.0.5; .NET CLR 1.1.4322; InfoPath.1)",
    "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; Deepnet Explorer 1.5.0; .NET CLR 1.0.3705)",
    "Mozilla/5.0 (Macintosh; U; Intel Mac OS X 10_5_6; en-us) AppleWebKit/525.27.1 (KHTML, like Gecko) Demeter/1.0.9 Safari/125",
    "Dillo/0.8.5",
    "Dillo/2.0",
    "Doris/1.15 [en] (Symbian)",
    "ELinks/0.13.GIT (textmode; Linux 2.6.22-2-686 i686; 148x68-3)",
    "ELinks/0.9.3 (textmode; Linux 2.6.11 i686; 79x24)",
    "Mozilla/5.0 (X11; U; Linux i686; en; rv:1.8.1.12) Gecko/20080208 (Debian-1.8.1.12-2) Epiphany/2.20",
    "Mozilla/5.0 (X11; U; Linux i686; en-US; rv:1.7.3) Gecko/20041007 Epiphany/1.4.7",
    "Mozilla/5.0 (Windows; U; Win95; en-US; rv:1.5) Gecko/20031007 Firebird/0.7",
    "Mozilla/5.0 (Windows; U; Win98; en-US; rv:1.5) Gecko/20031007 Firebird/0.7",
    "Mozilla/5.0 (Macintosh; U; Intel Mac OS X 10.5; ko; rv:1.9.1b2) Gecko/20081201 Firefox/3.1b2",
    "Mozilla/5.0 (Windows; U; Windows NT 5.1; cs; rv:1.9.0.8) Gecko/2009032609 Firefox/3.0.8",
    "Mozilla/5.0 (Windows; U; WinNT4.0; en-US; rv:1.7.9) Gecko/20050711 Firefox/1.0.5",
    "Mozilla/5.0 (X11; U; SunOS sun4u; en-US; rv:1.9b5) Gecko/2008032620 Firefox/3.0b5",
    "Mozilla/5.0 (X11; U; OpenBSD i386; en-US; rv:1.8.0.5) Gecko/20060819 Firefox/1.5.0.5",
    "Mozilla/5.0 (Macintosh; U; Intel Mac OS X 10.5; en-US; rv:1.9.1b3) Gecko/20090305 Firefox/3.1b3 GTB5",
    "Mozilla/5.0 (X11; U; Linux x86_64; en-US; rv:1.8.1.12) Gecko/20080214 Firefox/2.0.0.12",
    "Mozilla/5.0 (Windows; U; Windows NT 5.0; es-ES; rv:1.8.0.3) Gecko/20060426 Firefox/1.5.0.3",
    "Mozilla/5.0 (X11; U; Linux i686; en-US; rv:1.8.1.9) Gecko/20071113 BonEcho/2.0.0.9",
    "Mozilla/5.0 (X11; U; Linux i686; en-US; rv:1.8.1) Gecko/20061026 BonEcho/2.0",
    "Mozilla/5.0 (BeOS; U; Haiku BePC; en-US; rv:1.8.1.21pre) Gecko/20090227 BonEcho/2.0.0.21pre",
    "Mozilla/5.0 (X11; U; Linux i686; en-US; rv:1.9.0.8) Gecko/2009033017 GranParadiso/3.0.8",
    "Mozilla/5.0 (X11; U; Linux x86_64; en-US; rv:1.9.1b3pre) Gecko/20090109 Shiretoko/3.1b3pre",
    "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1b4pre) Gecko/20090311 Shiretoko/3.1b4pre",
    "Mozilla/5.0 (Macintosh; U; PPC Mac OS X Mach-O; en-US; rv:1.8.0.1) Gecko/20060314 Flock/0.5.13.2",
    "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.0.2) Gecko/2008092122 Firefox/3.0.2 Flock/2.0b3",
    "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US) AppleWebKit/525.13 (KHTML, like Gecko) Fluid/0.9.4 Safari/525.13",
    "Mozilla/5.0 (X11; U; Linux i686; en-US; rv:1.7.12) Gecko/20050929 Galeon/1.3.21",
    "Mozilla/5.0 (X11; U; Linux i686; en-US; rv:1.9.0.8) Gecko/20090327 Galeon/2.0.7",
    "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.0; GreenBrowser)",
    "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; .NET CLR 2.0.50727; GreenBrowser)",
    "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; .NET CLR 1.1.4322; InfoPath.1; .NET CLR 2.0.50727; .NET CLR 3.0.04506.30; GreenBrowser)",
    "HotJava/1.1.2 FCS",
    "Mozilla/3.0 (x86 [cs] Windows NT 5.1; Sun)",
    "Mozilla/5.1 (X11; U; Linux i686; en-US; rv:1.8.0.3) Gecko/20060425 SUSE/1.5.0.3-7 Hv3/alpha",
    "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; Trident/4.0; SIMBAR={CFBFDAEA-F21E-4D6E-A9B0-E100A69B860F}; Hydra Browser; .NET CLR 2.0.50727; .NET CLR 1.1.4322; .NET CLR 3.0.04506.30)",
    "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; Hydra Browser; .NET CLR 2.0.50727; .NET CLR 3.0.4506.2152; .NET CLR 3.5.30729)",
    "IBrowse/2.3 (AmigaOS 3.9)",
    "Mozilla/5.0 (compatible; IBrowse 3.0; AmigaOS4.0)",
    "Mozilla/4.5 (compatible; iCab 2.9.1; Macintosh; U; PPC)",
    "iCab/3.0.2 (Macintosh; U; PPC Mac OS X)",
    "iCab/4.0 (Macintosh; U; Intel Mac OS X)",
    "Mozilla/5.0 (Java 1.6.0_01; Windows XP 5.1 x86; en) ICEbrowser/v6_1_2",
    "ICE Browser/5.05 (Java 1.4.0; Windows 2000 5.0 x86)",
    "Mozilla/5.0 (X11; U; Linux i686; en-US; rv:1.8.1.9) Gecko/20071030 Iceape/1.1.6 (Debian-1.1.6-3)",
    "Mozilla/5.0 (X11; U; Linux x86_64; en-US; rv:1.8.1.8) Gecko/20071008 Iceape/1.1.5 (Ubuntu-1.1.5-1ubuntu0.7.10)",
    "Mozilla/5.0 (X11; U; Linux i686; en-US; rv:1.8.1.11) Gecko/20071203 IceCat/2.0.0.11-g1",
    "Mozilla/5.0 (X11; U; Linux i686; en-US; rv:1.9.0.3) Gecko/2008092921 IceCat/3.0.3-g1",
    "Mozilla/5.0 (X11; U; Linux i686; de; rv:1.9.0.5) Gecko/2008122011 Iceweasel/3.0.5 (Debian-3.0.5-1)",
    "Mozilla/5.0 (X11; U; Linux x86_64; en-US; rv:1.8.1.1) Gecko/20061205 Iceweasel/2.0.0.1 (Debian-2.0.0.1+dfsg-4)",
    "Mozilla/5.0 (X11; U; Linux i686; it; rv:1.9.0.5) Gecko/2008122011 Iceweasel/3.0.5 (Debian-3.0.5-1)",
    "Mozilla/4.0 (Windows; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 2.0.50727)",
    "Mozilla/4.0 (Mozilla/4.0; MSIE 7.0; Windows NT 5.1; FDM; SV1; .NET CLR 3.0.04506.30)",
    "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 1.0.3705; .NET CLR 1.1.4322; Media Center PC 4.0; .NET CLR 2.0.50727)",
    "Mozilla/4.0 (compatible; MSIE 5.0; Windows NT;)",
    "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.0; Trident/4.0; GTB5; SLCC1; .NET CLR 2.0.50727; Media Center PC 5.0; .NET CLR 3.0.04506; InfoPath.2; OfficeLiveConnector.1.3; OfficeLivePatch.0.0)",
    "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; iRider 2.21.1108; FDM)",
    "Mozilla/5.0 (Windows; U; Windows NT 6.1; en-US) AppleWebKit/528.7 (KHTML, like Gecko) Iron/1.0.155.0 Safari/528.7",
    "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US) AppleWebKit/525.19 (KHTML, like Gecko) Iron/0.2.152.0 Safari/12081672.525",
    "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US) AppleWebKit/528.5 (KHTML, like Gecko) Iron/0.4.155.0 Safari/528.5",
    "Mozilla/5.0 (Windows; U; Windows NT 6.1; en-US; rv:1.8.1.19) Gecko/20081217 K-Meleon/1.5.2",
    "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.8.0.5) Gecko/20060706 K-Meleon/1.0",
    "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.8.0.6) Gecko/20060731 K-Ninja/2.0.2",
    "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.8.1.2pre) Gecko/20070215 K-Ninja/2.1.1",
    "Mozilla/5.0 (Windows; U; Windows NT 5.1; zh-CN; rv:1.9) Gecko/20080705 Firefox/3.0 Kapiko/3.0",
    "Mozilla/5.0 (X11; Linux i686; U;) Gecko/20070322 Kazehakase/0.4.5",
    "Mozilla/5.0 (X11; U; Linux i686; en-US; rv:1.9.0.8) Gecko Fedora/1.9.0.8-1.fc10 Kazehakase/0.5.6",
    "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; KKman2.0)",
    "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; KKMAN3.2)",
    "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; KKman3.0)",
    "Mozilla/5.0 (compatible; Konqueror/2.2.1; Linux)",
    "Mozilla/5.0 (compatible; Konqueror/3.5; SunOS)",
    "Mozilla/5.0 (compatible; Konqueror/4.1; OpenBSD) KHTML/4.1.4 (like Gecko)",
    "Mozilla/5.0 (compatible; Konqueror/3.1-rc5; i686 Linux; 20020712)",
    "Links (0.96; Linux 2.4.20-18.7 i586)",
    "Links (0.98; Win32; 80x25)",
    "Links (2.1pre18; Linux 2.4.31 i686; 100x37)",
    "Links (2.1; Linux 2.6.18-gentoo-r6 x86_64; 80x24)",
    "Links (2.2; Linux 2.6.25-gentoo-r9 sparc64; 166x52)",
    "Mozilla/4.0 (compatible; MSIE 6.0; Linux 2.6.26-1-amd64) Lobo/0.98.3",
    "Mozilla/4.0 (compatible; MSIE 6.0; Windows XP 5.1) Lobo/0.98.4",
    "Mozilla/4.0 (compatible; Lotus-Notes/6.0; Windows-NT)",
    "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.9.1b3pre) Gecko/2008 Lunascape/4.9.9.98",
    "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US) AppleWebKit/528+ (KHTML, like Gecko, Safari/528.0) Lunascape/5.0.2.0",
    "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.0; .NET CLR 1.1.4322; Lunascape 2.1.3)",
    "Lynx/2.8.6rel.4 libwww-FM/2.14 SSL-MM/1.4.1 GNUTLS/1.6.3",
    "Lynx/2.8.3dev.6 libwww-FM/2.14",
    "Lynx/2.8.5dev.16 libwww-FM/2.14 SSL-MM/1.4.1 OpenSSL/0.9.7a",
    "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; SV1; Maxthon; .NET CLR 1.1.4322)",
    "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; MyIE2)",
    "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 5.1; Trident/4.0; .NET CLR 2.0.50727; MAXTHON 2.0)",
    "Midori/0.1.5 (X11; Linux; U; en-gb) WebKit/532+",
    "Mozilla/5.0 (X11; U; Linux i686; en-US; rv:1.0.1) Gecko/20020919",
    "Mozilla/5.0 (Windows; U; Windows NT 5.0; it-IT; rv:1.7.12) Gecko/20050915",
    "Mozilla/5.0 (Windows; U; Windows NT 5.0; en-US; rv:1.2.1; MultiZilla v1.1.32 final) Gecko/20021130",
    "Mozilla/5.0 (X11; U; Linux i686; en-US; rv:1.4; MultiZilla v1.5.0.0f) Gecko/20030624",
    "NCSA_Mosaic/2.0 (Windows 3.1)",
    "NCSA_Mosaic/3.0 (Windows 95)",
    "NCSA_Mosaic/2.6 (X11; SunOS 4.1.3 sun4m)",
    "Mozilla/3.01 (compatible; Netbox/3.5 R92; Linux 2.2)",
    "Mozilla/4.0 (compatible; MSIE 5.01; Windows NT 5.0; NetCaptor 6.5.0RC1)",
    "Mozilla/4.04 [en] (X11; I; IRIX 5.3 IP22)",
    "Mozilla/5.0 (Windows; U; Win 9x 4.90; de-DE; rv:0.9.2) Gecko/20010726 Netscape6/6.1",
    "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.8.1.12) Gecko/20080219 Firefox/2.0.0.12 Navigator/9.0.0.6",
    "Mozilla/4.08 [en] (WinNT; U ;Nav)",
    "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.0.2) Gecko/20030208 Netscape/7.02",
    "Mozilla/3.0 (Win95; I)",
    "Mozilla/4.51 [en] (Win98; U)",
    "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.7.5) Gecko/20060127 Netscape/8.1",
    "NetSurf/2.0 (RISC OS; armv3l)",
    "NetSurf/1.2 (Linux; i686)",
    "Mozilla/4.7 (compatible; OffByOne; Windows 2000)",
    "Mozilla/4.7 (compatible; OffByOne; Windows 98)",
    "OmniWeb/2.7-beta-3 OWF/1.0",
    "Mozilla/5.0 (Macintosh; U; PPC Mac OS X; en-US) AppleWebKit/420+ (KHTML, like Gecko, Safari) OmniWeb/v595",
    "Mozilla/4.5 (compatible; OmniWeb/4.1.1-v424.6; Mac_PowerPC)",
    "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1) Opera 7.10 [en]",
    "Opera/9.80 (Windows NT 5.1; U; cs) Presto/2.2.15 Version/10.00",
    "Opera/5.11 (Windows 98; U) [en]",
    "Opera/9.51 (Macintosh; Intel Mac OS X; U; en)",
    "Mozilla/4.0 (compatible; MSIE 5.0; Windows NT 4.0) Opera 6.01 [en]",
    "Opera/9.02 (Windows XP; U; ru)",
    "Mozilla/4.0 (compatible; MSIE 5.0; Windows 98) Opera 5.12 [en]",
    "Opera/9.70 (Linux i686 ; U; en) Presto/2.2.1",
    "Opera/7.03 (Windows NT 5.0; U) [en]",
    "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; en) Opera 9.24",
    "Opera/6.0 (Windows 2000; U) [fr]",
    "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.9.0.7) Gecko/2009030821 Firefox/3.0.7 Orca/1.1 build 2",
    "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.9.0.6) Gecko/2009022300 Firefox/3.0.6 Orca/1.1 build 1",
    "Mozilla/1.10 [en] (Compatible; RISC OS 3.70; Oregano 1.10)",
    "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; PhaseOut-www.phaseout.net)",
    "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.4a) Gecko/20030411 Phoenix/0.5",
    "Mozilla/5.0 (X11; U; Linux i686; en-US; rv:1.2b) Gecko/20021029 Phoenix/0.4",
    "Mozilla/5.0 (Windows; U; Windows NT 5.1; cs-CZ) AppleWebKit/527+ (KHTML, like Gecko)  QtWeb Internet Browser/2.5 http://www.QtWeb.net",
    "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US) AppleWebKit/527+ (KHTML, like Gecko) QtWeb Internet Browser/1.2 http://www.QtWeb.net",
    "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US) AppleWebKit/527+ (KHTML, like Gecko) QtWeb Internet Browser/1.7 http://www.QtWeb.net",
    "Mozilla/5.0 (Macintosh; U; PPC Mac OS X 10_5_6; it-it) AppleWebKit/528.16 (KHTML, like Gecko) Version/4.0 Safari/528.16",
    "Mozilla/5.0 (Windows; U; Windows NT 5.1; cs-CZ) AppleWebKit/523.15 (KHTML, like Gecko) Version/3.0 Safari/523.15",
    "Mozilla/5.0 (Macintosh; U; PPC Mac OS X; de-de) AppleWebKit/125.2 (KHTML, like Gecko) Safari/125.7",
    "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US) AppleWebKit/528.16 (KHTML, like Gecko) Version/4.0 Safari/528.16",
    "Mozilla/5.0 (Macintosh; U; PPC Mac OS X; fi-fi) AppleWebKit/420+ (KHTML, like Gecko) Safari/419.3",
    "Mozilla/5.0 (Macintosh; U; PPC Mac OS X; en-us) AppleWebKit/312.8 (KHTML, like Gecko) Safari/312.6",
    "Mozilla/5.0 (X11; U; Linux i686; rv:1.9.1a2pre) Gecko/20080824052448 SeaMonkey/2.0a1pre",
    "Mozilla/5.0 (Windows; U; Win 9x 4.90; en-GB; rv:1.8.1.6) Gecko/20070802 SeaMonkey/1.1.4",
    "Mozilla/5.0 (Windows; U; Windows NT 6.1; en-US; rv:1.9.1b3pre) Gecko/20081208 SeaMonkey/2.0a3pre",
    "Mozilla/5.0 (BeOS; U; BeOS BePC; en-US; rv:1.9a1) Gecko/20060702 SeaMonkey/1.5a",
    "Mozilla/5.0 (Macintosh; U; Intel Mac OS X 10.5; en-US; rv:1.9.1b3pre) Gecko/20081202 SeaMonkey/2.0a2",
    "Mozilla/5.0 (Macintosh; U; Intel Mac OS X; en-US; rv:1.8.1.13) Gecko/20080313 SeaMonkey/1.1.9",
    "Mozilla/5.0 (Macintosh; U; PPC Mac OS X; ja-jp) AppleWebKit/419 (KHTML, like Gecko) Shiira/1.2.3 Safari/125",
    "Mozilla/5.0 (Macintosh; U; PPC Mac OS X; en) AppleWebKit/417.9 (KHTML, like Gecko, Safari) Shiira/1.1",
    "Mozilla/5.0 (Macintosh; U; Intel Mac OS X; fr) AppleWebKit/418.9.1 (KHTML, like Gecko) Shiira Safari/125",
    "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1) Sleipnir/2.8.1",
    "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; .NET CLR 1.1.4322; InfoPath.1; .NET CLR 2.0.50727) Sleipnir/2.8.4",
    "Mozilla/5.0 (Macintosh; U; Intel Mac OS X 10_5_5; en-us) AppleWebKit/525.27.1 (KHTML, like Gecko) Stainless/0.4 Safari/525.20.1",
    "Mozilla/5.0 (Macintosh; U; Intel Mac OS X 10_5_6; en-us) AppleWebKit/528.16 (KHTML, like Gecko) Stainless/0.5.3 Safari/525.20.1",
    "Mozilla/5.0 (Macintosh; U; Intel Mac OS X 10_4_11; en) AppleWebKit/525.18 (KHTML, like Gecko) Sunrise/1.7.4 like Safari/4525.22",
    "Mozilla/5.0 (Macintosh; U; PPC Mac OS X; en-us) AppleWebKit/125.5.7 (KHTML, like Gecko) SunriseBrowser/0.853",
    "Mozilla/5.0 (X11; U; Linux i686; en-US; rv:1.9.0.10pre) Gecko/2009041814 Firefox/3.0.10pre (Swiftfox)",
    "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 2.0.50727; .NET CLR 3.0.04506.648; .NET CLR 3.5.21022; .NET CLR 1.1.4322; TheWorld)"};
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
                string safari = new Random().Next(170, 937).ToString()+"."+ new Random().Next(9, 56).ToString();
               // client.DefaultRequestHeaders.Add("User-Agent", $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/{safari} (KHTML, like Gecko) Chrome/{new Random().Next(50,140)}.0.0.0 Safari/{safari}");
                client.DefaultRequestHeaders.Add("User-Agent", users[ new Random().Next(0,users.Length)]);
                //client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
                //client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
                //client.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.9");
                //client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
                //client.DefaultRequestHeaders.Add("Sec-CH-UA", "\"Not A;Brand\";v=\"99\", \"Google Chrome\";v=\"109\", \"Chromium\";v=\"109\"");
                //client.DefaultRequestHeaders.Add("Sec-CH-UA-Mobile", "?0");
                //client.DefaultRequestHeaders.Add("Sec-CH-UA-Platform", "\"Windows\"");
                //client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
                //client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
                if (p == null) return false;
                var response = await client.GetAsync(p.url);
                if(response == null) return false;
                response.EnsureSuccessStatusCode();
                string content = await response.Content.ReadAsStringAsync();


                if (content.Length > 2000)
                {
                    currentPagesDownload++;
                    if(currentPagesDownload % 100 == 0) 
                    { 
                      //  Console.WriteLine($"\n\nProds[{Program.TS.products.Count}] [{pagesToParse.Count}] FPS: [{(100/byTen.Elapsed.TotalSeconds ).ToString("0.0")}]  :: {DateTime.Now}\n");byTen.Restart();
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

 