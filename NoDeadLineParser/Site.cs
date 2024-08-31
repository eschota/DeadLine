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
    public List<Page> LocalPages= new List<Page>();

    public DateTime LastParse
    {
        get
        {
            foreach (var item in products)
            {
                if (item.Pos.Last() < 100000) return item.ProductDate.Last();

            }
            return new DateTime(0, 0, 0);
        }
    }    
        public int TotalDays
    {
        get {
            foreach (var item in products)
            {
                if (item.Pos.Last() < 10000) return item.Pos.Count;
                
            }
            return 0;
        }
    }
        public List<Product> products = new List<Product>();
        public string SiteName ="TurboSquid";
        public string Url = "https://www.turbosquid.com/Search/3D-Models?media_typeid=2&page_num=1";
        
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
        //public Site (string _name, int _pages,string url = "https://www.turbosquid.com/Search/Index.cfm?page_num=1&size=100")
        public Site (string _name, int _pages,string url = "https://www.turbosquid.com/Search/3D-Models?media_typeid=2&page_num=1")
            {
                SiteName = _name;
                PagesToParse = _pages;
                Url = url;
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
                
                GetProducts();
       
        Worker.sites.Add(this);
                
    }

    public bool CheckPage(string p)
    {
        if (File.Exists(p)) return false;
        else return true;
    }
            public void FindPagesToParse()// Parse every page with minimal delay of 1-10 seconds, not more than 8 
             {

                pageIds.Clear();

                string isParseCompleted = Path.Combine(Paths.ParseFolder, RawFolder, System.Text.RegularExpressions.Regex.Replace(DateTime.Now.Date.ToString("dd/MM/yyyy"), "[\\/:*?\"<>|]", "_"), "Completed.log");
                if (File.Exists(isParseCompleted)) return;




                        for (int i = 1; i <= PagesToParse; i++) pageIds.Add(i);        
                        //pageIds = pageIds.OrderBy(x => Guid.NewGuid()).ToList();

                        foreach (int pageId in pageIds)
                        {
                            string url = Url.Replace("page_num=1", $"page_num={pageId}");
            
                            if(url.Contains("unity"))
                            url = Url.Replace("page=0", $"page={pageId}");
                            if (url == "") Console.WriteLine("Exception ebat!");
                            string filePage = Path.Combine(Paths.ParseFolder, RawFolder, System.Text.RegularExpressions.Regex.Replace(DateTime.Now.Date.ToString("dd/MM/yyyy"), "[\\/:*?\"<>|]", "_"), pageId.ToString("0000") + ".html");

                            if (!Directory.Exists(Path.GetDirectoryName(filePage))) Directory.CreateDirectory(Path.GetDirectoryName(filePage));

            if (!File.Exists(filePage)) // && !Worker.pagesToParse.Any(p => p.filePage == filePage || p.url == url))
            {
                if (SiteName != "TurboSquid")
                    Worker.pagesToParse.Add(new Worker.Page(url, this, filePage, pageType.Top));
                else LocalPages.Add((new Worker.Page(url, this, filePage, pageType.Top)));
            }
            

                        }
            //Console.WriteLine("Update Pages To Parse: " + pagesToParse.Count);
    }
    public async Task GetProducts()
    { 
            products.Clear();
        
            var files = Directory.GetFiles(ProductsFolder, "*.json");
        var tasks = new List<Task>();
        foreach (var file in files)
        {
            // Launching each file processing as a separate task
            tasks.Add(Task.Run(async () =>
            { 
                Product p = Product.Load(file); // Assuming Load is synchronous and quick, otherwise make it async
                if(p!=null) 
                if (p.ProductID == 0)
                {
                    int.TryParse(Path.GetFileNameWithoutExtension(file), out int k);
                    p.ProductID = k;
                    p.Save(file); // Assuming Save is synchronous and quick, otherwise make it async
                }
                lock (products) // Ensure thread-safe addition to the products list
                {      
                    if(p!=null)
                    products.Add(p);
                }
            }));
        }

        await Task.WhenAll(tasks);
        if (SiteName == "TurboSquid") CGtrends.GenerateProductsPage(products);
        Console.WriteLine($"Pages to Parse: {pagesToParse.Count}");
            if (products.Count > 0)
            {
                //Console.WriteLine($"Products: [{products.Count(x => x.Tags.Count > 0)}/{products.Count}]");
                Console.WriteLine($"Products with more than 1 day parse: {products.Count(x => x.Pos.Count > 1)}");
                Console.WriteLine($"Products with Certs:[ {products.Count(x => x.Certificate== Product.cert.No)}:{products.Count(x => x.Certificate >0)}]");
                Console.WriteLine($"Products with Id: {products.Count(x => x.ProductID>0)}");
                Console.WriteLine($"Products with Correct Submit Date: {products.Count(x => x.SubmitDate>new DateTime(1,1,1))}");
                Console.WriteLine($"Products with inCorrect Submit Date: {products.Count(x => x.ProductDate.Any(x=>x.Year==new DateTime(1,1,1).Year))}");
            }
        }
    
} 
    