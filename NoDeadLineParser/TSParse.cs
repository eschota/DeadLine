
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using OpenAIClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using static Worker;

internal static class TSParse
{
            private static List<string> pages= new List<string>();
            public static List<Product> products= new List<Product>();
      public static async void ParseTopPages(string[] dirs)
      {
        Stopwatch stopwatch = new Stopwatch();
        foreach (var item in dirs)
          {// get all pages to list
              pages.AddRange(Directory.GetFiles(item,"*.html"));
          }
        List<Task> tasks = new List<Task>();

        
        for (int i = 0; i < pages.Count; i++)
        {
            // Add a slight delay before starting each task to avoid overloading.
            await Task.Delay(50);

            // Using a local variable to avoid captured variable issue in loops.
            var page = pages[i];
            if (!File.Exists(page.Replace(".html", ".json")))
            {
                var task = Task.Run(async () => await ParseTopPagByID(page));
                if (i % 1000 == 0) Console.Write($"products:[{products.Count}]");
                tasks.Add(task);
            }
        }

        // Wait for all tasks to complete.
        await Task.WhenAll(tasks);
        //File.WriteAllText(Path.Combine(Paths.ParseFolder, Program.TS.RawFolder, System.Text.RegularExpressions.Regex.Replace(DateTime.Now.Date.ToString("dd/MM/yyyy"), "[\\/:*?\"<>|]", "_"), "Completed.log"),"");
        
    }
    public static async Task ParseTopPagByID(string page)
    {
        if (!File.Exists(page)) return;
        string html = File.ReadAllText(page);
         
            Product p = null;
        try
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            string pages = "";
            int PageID = 0;
            int.TryParse(Path.GetFileNameWithoutExtension(page), out PageID);
            PageID--;
            //int start = html.IndexOf("https://www.turbosquid.com/3d-models/");
            var productNodes = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'search-lab')]"); // XPath to select product nodes, adjust based on actual HTML
            if (productNodes != null)
            {
                for (int i = 0;i < productNodes.Count;i++) 
                {
                    var node = productNodes[i];
                    var product = new Product();



                    int id = -1;
                    int.TryParse(node.Id.Replace("Asset",""),out id);
                    if(id > 0) { product.ProductID = id; }

                    string filePage = Path.Combine( Program.TS.ProductsFolder, product.ProductID + ".json");
                    if (File.Exists(filePage))
                    {
                      Product  tproduct= Product.Load(filePage);
                        if(tproduct != null) { product = tproduct; }
                    }
                    

                    if (product.SubmitDate == new DateTime(1,1,1)) Worker.pagesToParse.Add(new Worker.Page(product.url, Program.TS, Path.Combine(Program.TS.ProductPagesFolder, product.ProductID + ".html"),Page.pageType.Product));
                    
                    DateTime newdate = File.GetCreationTime(page);
                    if (product.ProductDate.Count!=0 && (newdate - product.ProductDate.Last()).TotalHours <= 12) continue;
                    product.ProductDate.Add(newdate);
                    product.Pos.Add(PageID * 100 + i);
                    pages = pages + (PageID * 100 + i).ToString() + "\n";
                    var nameNode = node.SelectSingleNode(".//div[contains(@class, 'asset_name_item')]");
                    if (nameNode != null)
                    {
                        product.ProductName = nameNode.InnerText.Trim();
                    }

                    // Extracting product URL, adjust XPath based on actual HTML
                    var urlNode = node.SelectSingleNode(".//a[@href]");
                    if (urlNode != null)
                    {
                        product.url = urlNode.GetAttributeValue("href", "");
                    }

                    // Extracting main preview URL, adjust XPath based on actual HTML
                    var previewUrlNode = node.SelectSingleNode(".//img[contains(@class, '')]"); // Use a more specific class if possible
                    if (previewUrlNode != null)
                    {
                        product.ProductMainPreviewUrl = previewUrlNode.GetAttributeValue("src", "");
                    }

                    // Extracting price, adjust XPath based on actual HTML
                    var priceNode = node.SelectSingleNode(".//div[contains(@class, 'item_price')]//label");
                    if (priceNode != null)
                    {
                         
                        float price;
                        string priceText = priceNode.InnerText.Replace("$", "").Replace(",", "");
                        if (float.TryParse(priceText, out price))
                        {
                            product.Price.Add( price);
                        }
                    }
                    // Add more extraction logic as needed for other properties
                    //Console.WriteLine($"{product.ProductName} {product.ProductPrice} {product.ProductUrl} {product.ProductID}");
                    product.Save(filePage);
                    
                }
            }
            File.WriteAllText(page.Replace(".html", ".json"),pages);
            //if(File.Exists(page)) File.Delete(page);
        }
        catch (Exception ex)
        {
            Logger.AddLog($"Exception Parsing TS Product: {p}" + ex.Message, Logger.LogLevel.Exceptions);
        }
           
             
    }
    public static void GetProducts()
    {
        products.Clear();
        var files = Directory.GetFiles(Program.TS.ProductsFolder, "*.json");
        foreach (var file in files)
        {
            Product p = Product.Load(file);
            if(p.SubmitDate== new DateTime(1, 1, 1))
            { 
                products.Add(p);
                Worker.pagesToParse.Add(new Worker.Page(p.url, Program.TS, Path.Combine(Program.TS.ProductPagesFolder, p.ProductID + ".html"), Page.pageType.Product));
            }
            else 
                products.Add(p);
        }
        Console.WriteLine($"Pages to Parse: {pagesToParse.Count}");
        if (products.Count > 0)
        {
            Console.WriteLine($"Products: [{products.Count(x => x.Tags.Count > 0)}/{products.Count}]");
            Console.WriteLine($"Products with more than 1 day parse: {products.Count(x => x.Pos.Count > 1)}");
        }
    }
    public static async void GetNewProductAfterParse(string filePage)
    {        
            if (File.Exists(filePage))
            {
                await Task.Delay(100);
                Task.Run(async () => await ParseProductPage(filePage));
            }
    }
    private static async Task ParseProductPage(string page)
    {
        if (!File.Exists(page)) return;
        string html = File.ReadAllText(page);
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);

        Product product = new Product();

        

        string productJson = Path.Combine(Program.TS.ProductsFolder, Path.GetFileName( page).Replace(".html", ".json"));
        if (File.Exists(productJson))
        {
            product = Product.Load(productJson);
            if (product.ProductAuthor != "") return;
        }



        var titleNode = htmlDoc.DocumentNode.SelectSingleNode("//h1[@itemprop='name']");

        if (titleNode != null)
        {
            product.ProductName= titleNode.InnerText.Trim();
        }

        int id = -1;
        int.TryParse(page.Replace(".html", ""), out id);
        if (id != -1)
            product.ProductID = id;



        var tagContainer = htmlDoc.DocumentNode.SelectSingleNode("//div[@data-testid='tag-container']");

        if (tagContainer != null)
        {
            // Iterate through each anchor element within the div
            var tags = tagContainer.SelectNodes(".//a");

            if (tags != null)
            {
                product.Tags.Clear();
                foreach (var tag in tags)
                {
                    // Extract the text content of each anchor element
                    var tagName = tag.InnerText.Trim();
                    product.Tags.Add(tagName);
                }
            }
        }
        var descriptionNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@id='collapseExample']");

        if (descriptionNode != null)
        {
            // Replace <br> tags with \n for readability, if desired
            var descriptionHtml = descriptionNode.InnerHtml;
            var descriptionText = descriptionHtml.Replace("<br>", "\n").Trim();

            // Optionally, remove any remaining HTML tags and decode HTML entities
            product.Description = HtmlEntity.DeEntitize(descriptionText);
            
        }
        var artistLinkNode = htmlDoc.DocumentNode.SelectSingleNode("//a[@data-testid='artist-search-link']");

        if (artistLinkNode != null)
        {
            // Получаем имя артиста из атрибута 'title'
            var artistName = artistLinkNode.GetAttributeValue("title", string.Empty);
            if (artistName != null) product.ProductAuthor = artistName;
            // Получаем ссылку на профиль артиста из атрибута 'href'
            var artistProfileLink = artistLinkNode.GetAttributeValue("href", string.Empty);
              
        }
        var datePublishedNode = htmlDoc.DocumentNode.SelectSingleNode("//span[@id='FPDatePublished']");

        if (datePublishedNode != null)
        {
            try
            {
                product.SubmitDate= DateTime.ParseExact(datePublishedNode.InnerHtml.Trim(), "MMMM dd, yyyy", CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                Console.WriteLine($"Unable to parse '{datePublishedNode}'");
            }
        }
        var formatDivs = htmlDoc.DocumentNode.SelectNodes("//div[@data-testid[starts-with(., 'FPFormat_')]]");

        if (formatDivs != null)
        {
            product.Formats.Clear();
            foreach (var div in formatDivs)
            {
                var formatDetails = div.ChildNodes
                    .Where(node => node.Name == "span")
                    .Select(node => node.InnerText.Trim())
                    .Where(text => !string.IsNullOrEmpty(text));

                var formatStr = string.Join(" ", formatDetails);
                product.Formats.Add(formatStr);
            }
        }
        var textNodes = htmlDoc.DocumentNode.SelectNodes("//text()");

        // Фильтруем узлы, содержащие "CheckMate"
        var checkMateNodes = textNodes.Where(node => node.InnerText.Contains("CheckMate"));

        foreach (var node in checkMateNodes)
        {
            product.Certificate=node.InnerText.Trim();
        }
        if(product.SubmitDate!= new DateTime(1,1,1))
        product.Save(productJson);
        }
    } 