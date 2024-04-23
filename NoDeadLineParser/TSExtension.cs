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
            var scriptChart = HtmlNode.CreateNode("<script src=\"https://renderfin.com/graphicsTSExtension.js\" defer></script>");
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
                string canvasId = $"chart{p.ProductName.Replace(" ", "")}__id";
                string graph = ("<div class='myProduct'>");


                graph += "<script>Hello()</script>";
                graph += "<script>";
                graph += "document.addEventListener('DOMContentLoaded', function() {";
                graph += $"var posData = {posData};";
                graph += $"var dateData = {dateData};";
                graph += $"var container = document.getElementById('{canvasId}');";
                graph += "showChart(container, posData, dateData);";
                graph += "});";
                graph += "</script>";
                graph += $"<canvas id='{canvasId}'></canvas>";
                graph += "</div>";
                div.InnerHtml += graph;
            }
        }
        //var body = htmlDoc.DocumentNode.SelectSingleNode("//body");
        //if (body != null)
        //{
        //    var scriptTag = HtmlNode.CreateNode("<script src=\"https://renderfin.com/graphicsTSExtension.js\"></script>");
        //    body.AppendChild(scriptTag);
        //}
        return htmlDoc.DocumentNode.OuterHtml;
    }
} 
