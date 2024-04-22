using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class NodeStats
{
    public static List<NodeStats> Nodes
    {
        get
        {
            return LoadAndSortNodes();
        }
    }
    [JsonProperty("Time")]
    public long Time { get; set; }

    [JsonProperty("Info")]
    public NodeInfo Info { get; set; }

    [JsonProperty("Partial")]
    public Dictionary<string, HardwareLoadPartial> Partial { get; set; } 
public class NodeInfo
    {
        [JsonProperty("NodeName")]
        public string NodeName { get; set; }

        [JsonProperty("UserName")]
        public string UserName { get; set; }

        [JsonProperty("MachineName")]
        public string MachineName { get; set; }

        [JsonProperty("Version")]
        public string Version { get; set; }

        [JsonProperty("Ip")]
        public string Ip { get; set; }

        [JsonProperty("UPnpPort")]
        public int UPnpPort { get; set; }

        [JsonProperty("UPnpServerPort")]
        public int UPnpServerPort { get; set; }
    }
    public class HardwareLoadPartial
    {
        [JsonProperty("cpu")]
        public double CpuLoad { get; set; }

        [JsonProperty("gpu")]
        public double GpuLoad { get; set; }

        [JsonProperty("ram")]
        public long FreeRam { get; set; }

        [JsonProperty("iup")]
        public long InternetUp { get; set; }

        [JsonProperty("idown")]
        public long InternetDown { get; set; }
    }
    public static void WriteJson(string json)
    {
        try
        {
            var jsonObject = JObject.Parse(json);

            // Извлечение nodeName и Time динамически
            string nodeName = (string)jsonObject["Info"]["NodeName"];
            long time = (long)jsonObject["Time"];

            if (!string.IsNullOrEmpty(nodeName) && time > 0)
            {
                string fileName = Path.Combine( Paths.NodeStatsDirectory,$"{nodeName}${time}.json");
                File.WriteAllText(fileName, json);
                Console.WriteLine($"Data for {nodeName} written to {fileName}");
                GenerateHTML();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing JSON: {ex.Message}");
        }
    }

    private static List<NodeStats> LoadAndSortNodes()
    {
        List<NodeStats> nodes = new List<NodeStats>();
        
        var jsonFiles = Directory.GetFiles(Paths.NodeStatsDirectory, $"*.json");

        foreach (var file in jsonFiles)
        {
            string jsonContent = File.ReadAllText(file);
            var nodeStat = JsonConvert.DeserializeObject<NodeStats>(jsonContent);
            if (nodeStat != null)
            {
                nodes.Add(nodeStat);
            }
        }
       
        // Сортировка по времени
        nodes = nodes.OrderBy(node => node.Time).ToList();
        return nodes;
    }
    public static List<NodeStats> DistinctNodes(List<NodeStats> allNodes)
    {
         
        var nodesDictionary = new Dictionary<string, NodeStats>();

        foreach (var node in allNodes)
        {
            if (!nodesDictionary.ContainsKey(node.Info.NodeName))
            {
                nodesDictionary[node.Info.NodeName] = node;
            }
            else
            {
                // Агрегация данных загрузки оборудования
                var existingNode = nodesDictionary[node.Info.NodeName];
                foreach (var partial in node.Partial)
                {
                    if (!existingNode.Partial.ContainsKey(partial.Key))
                    {
                        existingNode.Partial.Add(partial.Key, partial.Value);
                    }
                    // Здесь можно добавить логику для объединения или обновления данных, если ключи совпадают
                }
            }
        }

        // Конвертация словаря обратно в список
        return nodesDictionary.Values.ToList();
    }
    public static void GenerateHTML()
    {
        List<NodeStats> nods = DistinctNodes(Nodes).ToList();
        var htmlBuilder = new StringBuilder();

        htmlBuilder.AppendLine("<!DOCTYPE html><html><head>");
        htmlBuilder.AppendLine("<meta http-equiv='refresh' content='10'>");
        htmlBuilder.AppendLine("<link rel=\"stylesheet\" href=\"stylesNodes.css\">");


        htmlBuilder.AppendLine("<title>RenderFin Nodes Stats</title>");
        htmlBuilder.AppendLine("<script src='https://cdnjs.cloudflare.com/ajax/libs/Chart.js/3.6.0/chart.js'></script>");
        htmlBuilder.AppendLine("<script src ='graphicsNodes.js'></script>");

        htmlBuilder.AppendLine("</head><body>");
        htmlBuilder.AppendLine($"<div class='header-banner'>Welcome to RenderFin Node Stats!<br> Last Update: {DateTime.Now}][Ticks: {NodeStats.Nodes.Count}] <br> Nodes Online: {nods.Count}</div>");
        htmlBuilder.AppendLine("<div class=\"grid-container\">");


       
            foreach (var nod in nods)
        {
            string chartData = GenerateChartData(nod);
            string canvasId = $"chart{nod.Info.NodeName.Replace(" ", "_")}";

            htmlBuilder.AppendLine("<div class='product'>");
            htmlBuilder.AppendLine($"<canvas id='{canvasId}'></canvas>");
            htmlBuilder.AppendLine($"<script>");
            htmlBuilder.AppendLine($"document.addEventListener('DOMContentLoaded', function() {{");
            htmlBuilder.AppendLine($"var chartData = {chartData};");
            htmlBuilder.AppendLine($"var container = document.getElementById('{canvasId}');");
            htmlBuilder.AppendLine($"showChart(container, chartData.data, chartData.labels);");
            htmlBuilder.AppendLine("});");
            htmlBuilder.AppendLine("</script>");
            htmlBuilder.AppendLine(Title(nod, 0, 1));
            htmlBuilder.AppendLine("</div>"); // close product

        }
        htmlBuilder.AppendLine("</div>");

        htmlBuilder.AppendLine("</body></html>");



        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Nodes.html"), htmlBuilder.ToString());
    }
    
    
    public static string Title(NodeStats p, int min, int max)
    {
        string Title = p.Info.NodeName+$"  {p.Info.MachineName}";
        if (Title.Length > 20) Title = Title.Substring(0, 20) + "...";

        string startColor = "#FF0000"; // Синий в HEX
        string endColor = "#00ff0d"; // Красный в HEX

        // Рассчитываем доли для начального и конечного цветов
        //double fractionLast = (p.Pos.Last() - min) / (double)(max - min); // Для p.Pos.Last()
        //double fractionFirst = (p.Pos.First() - min) / (double)(max - min); // Для p.Pos.First()
        string colorA = "rgb(255,0,0)";
        string colorB = "rgb(0,255,0)";
       // if (p.Pos.Last() <= p.Pos.First())
        {
            colorA = "rgb(0,255,0)";
            colorB = "rgb(255,0,0)";
        }
        //double fractionFirst = (p.Pos.First() - min) / (double)(max - min); // Для p.Pos.First()
        //double fractionLast = (p.Pos.Last() - min) / (double)(max - min); // Для p.Pos.Last()

        //string colorA = InterpolateColorSimple(startColor, endColor, fractionFirst);
        //string colorB = InterpolateColorSimple(startColor, endColor, fractionLast);

        string Link = $"<div class='title' style='background: linear-gradient(to right, {colorB}, {colorA}); color: white; text-decoration: none;'><p><a href='{"href"}' style='color: white; text-decoration: none;'>{Title}</a></p></div>";


        // Возвращаем сформированную строку с HTML
        return Link;
    }
    public static string GenerateChartData(NodeStats node)
    {
        // Сортировка ключей для обеспечения правильной последовательности времени на графике
        var sortedPartials = node.Partial.OrderBy(p => p.Key);

        // Взять последние 20 записей
        var lastPartials = sortedPartials.TakeLast(30);

        List<string> labels = new List<string>();
        List<double> data = new List<double>();

        foreach (var partial in lastPartials)
        {
            DateTime time = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(partial.Key)).LocalDateTime;
            labels.Add(time.ToString("yyyy-MM-dd HH:mm:ss"));
            data.Add(partial.Value.CpuLoad);
        }

        var jsonData = JsonConvert.SerializeObject(new { labels, data });
        return jsonData;
    }
}
