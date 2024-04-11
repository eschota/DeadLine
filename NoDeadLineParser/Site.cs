using OpenAIClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Worker;
using static Worker.Page;

 
    public class Site
    {
        public string SiteName ="TurboSquid";
        public string Url = "https://www.turbosquid.com/Search/Index.cfm?page_num=1&size=200";

        public int PagesToParse = 500;
        public string RawFolder => Path.Combine(AppContext.BaseDirectory, "Parse", SiteName, "RawData");
        public string ProductsFolder => Path.Combine(AppContext.BaseDirectory, "Parse", SiteName, "Products");
        public string [] RawFoldersAll
    {
        get
        {
            string all =Path.Combine(AppContext.BaseDirectory, "Parse", SiteName, "RawData");
            return Directory.GetDirectories(all);

        }
    }
        public string ProductPagesFolder => Path.Combine(AppContext.BaseDirectory, "Parse", SiteName, "ProductPages");

        public List<int> pageIds = new List<int>(); 
        public Site (string _name, int _pages,string url = "https://www.turbosquid.com/Search/Index.cfm?page_num=1&size=200")
            {

                if (!Directory.Exists(RawFolder))
                {
                    Directory.CreateDirectory(RawFolder);
                }
                if (!Directory.Exists(ProductPagesFolder))
                {
                    Directory.CreateDirectory(ProductPagesFolder);
                }
                if (!Directory.Exists(ProductsFolder))
                {
                    Directory.CreateDirectory(ProductsFolder);
                }
                SiteName = _name;
                PagesToParse = _pages;
                Url = url;
                Worker.sites.Add(this);
                
    }
            public void FindPagesToParse()// Parse every page with minimal delay of 1-10 seconds, not more than 8 
            {
                string isParseCompleted = Path.Combine(Paths.ParseFolder, RawFolder, System.Text.RegularExpressions.Regex.Replace(DateTime.Now.Date.ToString("dd/MM/yyyy"), "[\\/:*?\"<>|]", "_"), "Completed.log");
                if (File.Exists(isParseCompleted)) return;




        for (int i = 1; i <= PagesToParse; i++) pageIds.Add(i);        
                        pageIds = pageIds.OrderBy(x => Guid.NewGuid()).ToList();

                        foreach (int pageId in pageIds)
                        {
                            string url = Url.Replace("page_num=1", $"page_num={pageId}");
            
                            if(url.Contains("unity"))
                            url = Url.Replace("page=0", $"page={pageId}");
            if (url == "") Console.WriteLine("Exception ebat!");
            string filePage = Path.Combine(Paths.ParseFolder, RawFolder, System.Text.RegularExpressions.Regex.Replace(DateTime.Now.Date.ToString("dd/MM/yyyy"), "[\\/:*?\"<>|]", "_"), pageId.ToString("0000") + ".html");

                            if (!Directory.Exists(Path.GetDirectoryName(filePage))) Directory.CreateDirectory(Path.GetDirectoryName(filePage));

            if (!File.Exists(filePage)  && !Worker.pagesToParse.Any(p => p.filePage == filePage || p.url == url))
            {
                Worker.pagesToParse.Add(new Worker.Page(url, this, filePage, pageType.Top));
            }
            

                        }
            //Console.WriteLine("Update Pages To Parse: " + pagesToParse.Count);
    }
    
} 
    