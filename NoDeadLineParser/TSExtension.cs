using HtmlAgilityPack;
using Microsoft.AspNetCore.Http;
using System;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

 internal class TSExtension
{
    static string baseDiv = "";
    private static string ShowTrends(int? number, string html)
    {
        
        int num = number ?? 200; 
        DateTime currentDate = DateTime.Now;
        var filteredSortedProducts = Program.TS.products
        .Where(p => (currentDate - p.SubmitDate).Days <= num) // Оставляем продукты, SubmitDate которых не старше num дней от текущей даты
        .OrderByDescending(p => p.Pos.LastOrDefault()) // Сортировка по последнему значению в Pos
        .ToList();
        if( filteredSortedProducts.Count>200 ) num = 200;
        else num = filteredSortedProducts.Count;
        

        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);

        var divss = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'product_search_result')]");

        if (divss != null && divss.Count > 1) // Проверяем, есть ли более одного элемента
        {
            // Пропускаем первый элемент и удаляем все остальные
            foreach (var div in divss.Skip(1))
            {
                div.Remove();
            }
        }

        var divs = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'product_search_result')]");
        if (divs == null || divs.Count == 0) return html; // Если нет таких divs, возвращаем исходный HTML

        // Выбираем родительский узел для добавления копий
        var parentNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@id='formats-menu']");

        // Рассчитываем, сколько копий каждого из найденных divs необходимо создать
        int copiesNeeded = num / divs.Count;
        var extraCopies = num % divs.Count; // Для равномерного распределения дополнительных копий

        foreach (var div in divs)
        {
            // Для каждого div создаем указанное количество копий
            for (int i = 0; i < copiesNeeded; i++)
            {
                var copy = HtmlNode.CreateNode(div.OuterHtml);
                parentNode.AppendChild(copy);
            }
            // Добавляем одну дополнительную копию, если это необходимо для достижения общего числа в 200
            if (extraCopies > 0)
            {
                var extraCopy = HtmlNode.CreateNode(div.OuterHtml);
                parentNode.AppendChild(extraCopy);
                extraCopies--;
            }
        }
        htmlDoc=replaceProducts(htmlDoc,filteredSortedProducts);
        return TSProductSearch(htmlDoc.DocumentNode.OuterHtml);
    }
    private static HtmlDocument replaceProducts(HtmlDocument htmlDoc, List<Product> prods)
    {

        var divs = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'product_search_result')]");
        if (divs == null) return null;
        foreach (var div in divs)
        {
            string tsId = div.GetAttributeValue("data-ts-id", string.Empty);
            int k = -1;
            if (tsId != string.Empty)
            {
                int.TryParse(tsId, out k);
            }
            if (k == -1) continue;
            Product p = prods.FindLast(x => x.ProductID == k);

        }

            return htmlDoc;
    }
    public static string TSProductSearch(string html)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);

        var divs = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'product_search_result')]");
        if (divs == null) return null;
        foreach (var div in divs)
        { 
            string tsId = div.GetAttributeValue("data-ts-id", string.Empty);
            int k = -1;
            if (tsId != string.Empty)
            {
                
                int.TryParse(tsId, out k);
            }
            if (k == -1) continue;
            Product p = Program.TS.products.FindLast(x => x.ProductID == k);
            if (p != null)
            {
              //  div.InnerHtml += CGtrends.GenerateBage(p);
            }
        }

        return htmlDoc.DocumentNode.OuterHtml;
    }
     

    
    public static string TSProductTops(string html)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);

        var divs = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'product_search_result')]");
        if (divs == null) return null;
        foreach (var div in divs)
        {
            string tsId = div.GetAttributeValue("data-ts-id", string.Empty);
            int k = -1;
            if (tsId != string.Empty)
            {

                int.TryParse(tsId, out k);
            }
            if (k == -1) continue;
            Product p = Program.TS.products.FindLast(x => x.ProductID == k);
            if (p != null)
            {
                CGtrends.Title(p,0,1000);

                var newHtml = $@" <div style='position: absolute; top: 10px; left: 10px; background-color: rgba(0,0,0,1); color: white; padding: 5px;'>{p.Pos.Last()}</div>
                                 <div style='position: absolute; top: 50px; left: 10px; background-color: rgba(0,0,0,1); color: white; padding: 5px;'>{p.Pos.First()}</div>";
                                // + Date;


                div.InnerHtml += newHtml;
            }
        }

        return htmlDoc.DocumentNode.OuterHtml;
    }
 

    public static string ProcessKeyword(string url,string html)
    {
        var uri = new Uri(url);
        var query = HttpUtility.ParseQueryString(uri.Query);
        var keyword = query["keyword"];

        // Пытаемся найти ключевое слово и числовое значение или диапазон
        if (keyword.StartsWith("top", StringComparison.OrdinalIgnoreCase))
        {
            return ShowTrends(ExtractNumber(keyword), html);
        }
        else if (keyword.StartsWith("trend", StringComparison.OrdinalIgnoreCase))
        {
            return ShowTrends(ExtractNumber(keyword), html);
        }else
        if (keyword.StartsWith("top", StringComparison.OrdinalIgnoreCase))
        {
            return ShowTrends(ExtractNumber(keyword), html);
        }
        else
        {
            var range = ExtractRange(keyword);
            if (range != null)
            {
                return $"Handled range {range.Item1}-{range.Item2}";
            }
        }

        return $"No matching handler found for keyword: {keyword}";
    }

    private static string HandleTop(int? number) => $"Handled 'top' with number {number}";
    

    // Извлекаем числовое значение из строки
    private static int? ExtractNumber(string keyword)
    {
        var numberString = System.Text.RegularExpressions.Regex.Match(keyword, @"\d+").Value;
        if (int.TryParse(numberString, out var number))
        {
            return number;
        }
        return null;
    }

    // Пытаемся извлечь диапазон в формате "начало-конец"
    private static Tuple<int, int>? ExtractRange(string keyword)
    {
        var match = System.Text.RegularExpressions.Regex.Match(keyword, @"(\d+)-(\d+)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out var start) && int.TryParse(match.Groups[2].Value, out var end))
        {
            return Tuple.Create(start, end);
        }
        return null;
    }
} 
