using HtmlAgilityPack;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web; 

 internal class TSExtension
{
    public class ProductContainer
    {
        public int InstallId { get; set; }  // Новое поле для installId
        public Product[] Products { get; set; }
    }
    public static string GetTrendsByIds(Product[] products)
    {
        var matchedProducts = Program.TS.products
         .Where(p => products.Select(x => x.ProductID).Contains(p.ProductID))
         .Select(p => new Product
         {
             Div = p.Div,
             ProductID = p.ProductID,
             ProductName = p.ProductName,
             url = p.url,
             ProductMainPreviewUrl = p.ProductMainPreviewUrl,
             Price = p.Price,
             Pos = p.Pos,
             // Обновляем каждую дату, удаляя время
             ProductDate = p.ProductDate.Select(d => d.Date).ToList(),
             ProductAuthor = p.ProductAuthor,
             SubmitDate = p.SubmitDate,
             Formats = p.Formats,
             Certificate = p.Certificate,
             AuthorLink = p.AuthorLink,
             Description = p.Description,
             Tags = p.Tags
         }).ToList();

        return JsonConvert.SerializeObject(matchedProducts);
    }
    public static string TSProductSearch(string html)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);

        var head = htmlDoc.DocumentNode.SelectSingleNode("//head");
        if (head != null)
        {
            // Добавление CSS стилей
            var linkTag = HtmlNode.CreateNode("<link rel=\"stylesheet\" href=\"https://renderfin.com/stylesTSExtension.css\">");
            head.AppendChild(linkTag);

            // Добавление основного скрипта для Chart.js
            var scriptTag = HtmlNode.CreateNode("<script src='https://cdnjs.cloudflare.com/ajax/libs/Chart.js/3.6.0/chart.js'></script>");
            head.AppendChild(scriptTag);

            // Добавление вашего скрипта с атрибутом defer
            var scriptChart = HtmlNode.CreateNode("<script src='https://renderfin.com/graphicsTSExtension.js'></script>");
            head.AppendChild(scriptChart);
        }
        var divs = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'flex')]");
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
                HtmlNode parentNode = div.ParentNode;
                    
                div.InnerHtml += CGtrends.GenerateDay(p);
                div.InnerHtml += CGtrends.Title(p,0,1);
                div.InnerHtml += $@"<div class='pos-badge'>{p.Pos.Last().ToString("#,0", System.Globalization.CultureInfo.InvariantCulture).Replace(",", " ")}</div>";
                div.InnerHtml += $@"<div class='posLast-badge'>{p.Pos.First().ToString("#,0", System.Globalization.CultureInfo.InvariantCulture).Replace(",", " ")}</div>";
                string posData = Newtonsoft.Json.JsonConvert.SerializeObject(p.Pos);
                string dateData = JsonConvert.SerializeObject(p.ProductDate.Select(d => d.ToString("MM-dd")));



                div.InnerHtml+=("<div class='ext_product'>");

                div.InnerHtml+=($"<div class='imagegraph' onmouseover='showChart(this, {posData},{dateData})' onmouseout='hideChart(this)'>");
               

                // Добавляем canvas для графика с исходным размером
                //htmlBuilder.AppendLine("<div style=\"position: absolute; bottom: -5px; left: -5px; width: calc(100% + 10px); height: 30px; background-color: rgba(0,0,0,0.6); color: white; padding: 0px; box-sizing: border-box;\"></div>");

                div.InnerHtml += ("<canvas class='chart-canvas'></canvas>");
                div.InnerHtml += ("</div>");
                
            }
        }
        var body = htmlDoc.DocumentNode.SelectSingleNode("//body");
        if (body != null)
        {
            var scriptTag = HtmlNode.CreateNode("<script src=\"https://renderfin.com/graphicsTSExtension.js\"></script>");
            body.AppendChild(scriptTag);
            
            body.AppendChild(HtmlNode.CreateNode("<script>alert('Hello! I am an alert box!!');</script>"));
        }
        return htmlDoc.DocumentNode.OuterHtml;
    }
} 
