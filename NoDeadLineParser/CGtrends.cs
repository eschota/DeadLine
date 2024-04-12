using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;


internal class CGtrends
{
    static StringBuilder htmlBuilder = new StringBuilder();
    public static void GenerateProductsPage(List<Product> products)
    {
        DateTime currentDate = DateTime.Now;
        var filteredSortedProducts = products
            .Where(p => (currentDate - p.SubmitDate).Days <= 14 && p.Certificate == "")
            //.OrderByDescending(p => p.Pos.LastOrDefault())
            .OrderBy(p => Guid.NewGuid()) 
            .TakeLast(200)
            .ToList();

        var htmlBuilder = new StringBuilder();

        htmlBuilder.AppendLine("<!DOCTYPE html><html><head>");
        
        htmlBuilder.AppendLine("<link rel=\"stylesheet\" href=\"styles.css\">");
        

        htmlBuilder.AppendLine("<title>Turbosquid Trends</title>"); 
        htmlBuilder.AppendLine("<script src='https://cdnjs.cloudflare.com/ajax/libs/Chart.js/3.6.0/chart.js'></script>");
        htmlBuilder.AppendLine("<script src ='graphics.js'></script>");

        htmlBuilder.AppendLine("</head><body>");

        htmlBuilder.AppendLine("<div class=\"grid-container\">");

        foreach (var product in filteredSortedProducts)
        {
            string posData = Newtonsoft.Json.JsonConvert.SerializeObject(product.Pos);
            

        htmlBuilder.AppendLine("<div class='product'>");

            htmlBuilder.AppendLine("<div class='imgagegraph'>");
            htmlBuilder.AppendLine($"<img src='{product.ProductMainPreviewUrl}' alt='{product.ProductName + "  "}' onclick='window.open(this.src, \"_blank\")asd;' onmouseover='showChart(this, {posData})' onmouseout='hideChart(this)' style='width: 100%;'/>");
            // Добавляем canvas для графика с исходным размером
            //htmlBuilder.AppendLine("<div style=\"position: absolute; bottom: -5px; left: -5px; width: calc(100% + 10px); height: 30px; background-color: rgba(0,0,0,0.6); color: white; padding: 0px; box-sizing: border-box;\"></div>");

            htmlBuilder.AppendLine("<canvas class='chart-canvas'></canvas>");
            htmlBuilder.AppendLine("</div>");


            htmlBuilder.AppendLine("<div class='badges'>");
                htmlBuilder.AppendLine(Title(product, filteredSortedProducts.Min(p => p.Pos.Last()), filteredSortedProducts.Max(p => p.Pos.Last())));
                htmlBuilder.AppendLine($"<div class='price-badge'>{product.Price.LastOrDefault()}$</div>");
                htmlBuilder.AppendLine($@"<div class='pos-badge'>{product.Pos.Last()}</div>");
                htmlBuilder.AppendLine($@"<div class='posLast-badge'>{product.Pos.First()}</div>");
                //htmlBuilder.AppendLine(GenerateDay(product));

                htmlBuilder.AppendLine("</div>");

        htmlBuilder.AppendLine("</div>");// close product

        }
        htmlBuilder.AppendLine("</div>"); 

        htmlBuilder.AppendLine("</body></html>");

        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "cgtrends.html"), htmlBuilder.ToString());
    }

     
    public static string GenerateDay(Product p)
    {
        TimeSpan timeSpan = DateTime.Now.Subtract(p.SubmitDate);
        int days = timeSpan.Days;

        // Determine the background color and text based on the age of the file
        string backgroundColor = "rgba(0,0,0,1)"; // Default background color
        string text = days.ToString()+" Days";

        if (days <= 30)
        {
            backgroundColor = "rgba(0,125,0,1)"; // Green with transparency
        }
        else if (days > 30 && days <= 180)
        {
            backgroundColor = "rgba(0,255,255,1)"; // Yellow with transparency
        }
        else if (days > 180 && days <= 365)
        {
            backgroundColor = "rgba(255,165,0,1)"; // Orange with transparency
        }
        else if (days > 365)
        {
            // Calculate years with 1 decimal place
            double years = Math.Round(days / 365.0, 1);
            text = $"{years} Years";

            // Change color from red to dark brown based on the number of years, maxing out at 3 years
            int blue = 255 - (int)Math.Min(255, (years / 3) * 255);
            int green = (int)Math.Max(0, 40 - (years / 3) * 40);
            int red = 0; // Keeping blue at 0 to blend towards brown
            backgroundColor = $"rgba({red},{green},{blue},0.5)"; // Interpolated color with transparency
        }
        return $@"<div class='days'>{text}</div>"");";
    }
    public static string Title(Product p, int min, int max)
    {
        string Title = p.ProductName;
        if (Title.Length > 20) Title = Title.Substring(0, 20) + "...";

        string startColor = "#FF0000"; // Синий в HEX
        string endColor = "#00ff0d"; // Красный в HEX

        // Рассчитываем доли для начального и конечного цветов
        double fractionLast = (p.Pos.Last() - min) / (double)(max - min); // Для p.Pos.Last()
        double fractionFirst = (p.Pos.First() - min) / (double)(max - min); // Для p.Pos.First()
        string colorA = InterpolateColorSimple(startColor, endColor, fractionFirst);
        string colorB = InterpolateColorSimple(startColor, endColor, fractionLast);

        string Link = $"<div class='title' style='background: linear-gradient(to right, {colorA}, {colorB}); color: white; text-decoration: none;'><p><a href='{p.url}' style='color: white; text-decoration: none;'>{Title}</a></p></div>";


        // Возвращаем сформированную строку с HTML
        return Link;
    }  
    public static string InterpolateColorSimple(string startColor, string endColor, double fraction)
    {
        // Парсинг цветов
        Color start = ColorTranslator.FromHtml(startColor);
        Color end = ColorTranslator.FromHtml(endColor);

        // Расчет нового цвета
        int red = (int)(start.R + (end.R - start.R) * fraction);
        int green = (int)(start.G + (end.G - start.G) * fraction);
        int blue = (int)(start.B + (end.B - start.B) * fraction);

        // Форматирование в HEX (поддержка CSS)
        string hexColor = $"#{red:X2}{green:X2}{blue:X2}";
        return hexColor;
    }
}

