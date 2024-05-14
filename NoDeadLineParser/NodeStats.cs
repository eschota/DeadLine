using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
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
    
    public List<CompletedTasks> completedTasks = new List<CompletedTasks> ();

    public class CompletedTasks
    {
        public DateTime Time { get; set; }
        public string taskType { get; set; }
    }

    public class Validation
    {
        [JsonProperty("Id")]
        public string Id { get; set; }

        [JsonProperty("State")]
        public string State { get; set; }

        [JsonProperty("Type")]
        public string Type { get; set; }
         
        
    }
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
  
    public class HardwareLoad
    {
        [JsonProperty("Load")]
        public HardwareLoadPartial Load { get; set; }

        [JsonProperty("Drives")]
        public Dictionary<string, DriveDetails> Drives { get; set; }
    }

    public class DriveDetails
    {
        [JsonProperty("FreeSpace")]
        public long FreeSpace { get; set; }
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
                GenerateHTML(0);
                GenerateHTML(1);
                GenerateHTML(2);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing JSON: {ex.Message}");
        }
    }
    static int countTasks= 0;
    private static List<NodeStats> LoadAndSortNodes()
    {
        countTasks = 0;
        List<NodeStats> nodes = new List<NodeStats>();
        
        var jsonFiles = Directory.GetFiles(Paths.NodeStatsDirectory, $"*.json");

        foreach (var file in jsonFiles)
        {
            string js = File.ReadAllText(file);
            var nodeStat = JsonConvert.DeserializeObject<NodeStats>(js);
            if (nodeStat != null)
            {
                

                int k = -1;
                k=js.IndexOf("Validation\":[");
                if (k > -1)
                {
                    js=js.Substring( k + 12);
                    int l = js.IndexOf("]");
                    js=js.Substring(0,l+1);
                    JArray items = JArray.Parse(js);

                    foreach (var item in items)
                    {
                        CompletedTasks ct = new CompletedTasks();
                        ct .taskType= item["Type"].ToString();

                        // Получение значения Validation из вложенного объекта Times
                        var times = item["Times"];
                        var validationValue = times["Validation"];

                        string validationDate = "Validation time is not set";
                        if (validationValue.Type != JTokenType.Null)
                        {
                            long unixTimeMilliseconds = (long)validationValue;
                            ct.Time = DateTimeOffset.FromUnixTimeMilliseconds(unixTimeMilliseconds).DateTime.ToLocalTime();
                            
                        }
                        nodeStat.completedTasks.Add(ct);
                        countTasks++;
                    }
                   
                }
                nodes.Add(nodeStat);
            }
        }
       
        // Сортировка по времени
        nodes = nodes.OrderBy(node => node.Time).ToList();
        Console.WriteLine("CountTasks: " + countTasks);
        return nodes;
    }
    public static List<NodeStats> DistinctNodes(List<NodeStats> allNodes)
    {
        var nodesDictionary = new Dictionary<string, NodeStats>();

        foreach (var node in allNodes)
        {
            if (!nodesDictionary.ContainsKey(node.Info.NodeName))
            {
                // Если узел с таким именем не существует в словаре, добавляем его
                nodesDictionary[node.Info.NodeName] = node;
            }
            else
            {
                // Если узел уже существует, объединяем данные
                var existingNode = nodesDictionary[node.Info.NodeName];

                // Агрегация данных загрузки оборудования
                foreach (var partial in node.Partial)
                {
                    if (!existingNode.Partial.ContainsKey(partial.Key))
                    {
                        existingNode.Partial.Add(partial.Key, partial.Value);
                    }
                }

                // Объединение списка задач
                if (node.completedTasks != null)
                {
                    existingNode.completedTasks.AddRange(node.completedTasks);
                }
            }
        }

        // Конвертация словаря обратно в список
        return nodesDictionary.Values.ToList();
    }
    public static List<NodeStats> DistinctNodesOLD(List<NodeStats> allNodes)
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
                var existingNode = nodesDictionary[node.Info.NodeName];

                // Агрегация данных загрузки оборудования
                foreach (var partial in node.Partial)
                {
                    if (!existingNode.Partial.ContainsKey(partial.Key))
                    {
                        existingNode.Partial.Add(partial.Key, partial.Value);
                    }
                    // Можно добавить логику для объединения или обновления данных, если ключи совпадают
                }

                // Агрегация данных о завершённых задачах
                if (node.completedTasks != null)
                {
                    existingNode.completedTasks.AddRange(node.completedTasks);
                    // Удаление дубликатов может потребоваться, если одна и та же задача может быть повторена
                    existingNode.completedTasks = existingNode.completedTasks
                        .GroupBy(task => task.taskType)
                        .Select(group => new CompletedTasks
                        {
                            Time = group.Max(task => task.Time),
                            taskType = group.Key
                        }).ToList();
                }
            }
        }

        // Конвертация словаря обратно в список
        return nodesDictionary.Values.ToList();
    }
    public static void GenerateHTML(int type)
    {
        List<NodeStats> nods = DistinctNodes(Nodes).ToList();
        var htmlBuilder = new StringBuilder();

        htmlBuilder.AppendLine("<!DOCTYPE html><html><head>");
        htmlBuilder.AppendLine($"<meta http-equiv='refresh' content='{10+(type*30)}'>");
        htmlBuilder.AppendLine("<link rel=\"stylesheet\" href=\"stylesNodes.css\">");


        htmlBuilder.AppendLine("<title>RenderFin Nodes Stats</title>");
        htmlBuilder.AppendLine("<script src='https://cdnjs.cloudflare.com/ajax/libs/Chart.js/3.9.1/chart.js'></script>");
        htmlBuilder.AppendLine("<script src ='graphicsNodes.js'></script>");

        htmlBuilder.AppendLine("</head><body>");

        htmlBuilder.AppendLine($"<div class='header-banner'>Welcome to RenderFin Node Stats!<br> Last Update: {DateTime.Now}][Ticks: {NodeStats.Nodes.Count}] <br> Nodes Online: {nods.Count}            [Total Tasks Completed: {nods.SelectMany(nod => nod.completedTasks).Count()}]");
        htmlBuilder.AppendLine("<div class='button-container'>");
        htmlBuilder.AppendLine($"<a href='https://renderfin.com/nodes.html' class='button{(type == 0 ? " active" : "")}'>Realtime</a>");
        htmlBuilder.AppendLine($"<a href='https://renderfin.com/nodesByHour.html' class='button{(type == 1 ? " active" : "")}'>Last Hour</a>");
        htmlBuilder.AppendLine($"<a href='https://renderfin.com/NodesAllTime.html' class='button{(type == 2 ? " active" : "")}'>AllTime</a>");
        htmlBuilder.AppendLine("</div>");
        htmlBuilder.AppendLine("</div>");

        htmlBuilder.AppendLine("<div class=\"grid-container\">");


       
            foreach (var nod in nods)
        {

            string chartData = "";
            if(type==0) chartData =GenerateChartData(nod);
            if(type==1) chartData =GenerateAveragedByHourChartData(nod);
            if(type==2) chartData =GenerateAveragedAllTimeChartData(nod);

            string canvasId = $"chart{nod.Info.NodeName.Replace(" ", "_")}";

            htmlBuilder.AppendLine("<div class='product'>");
            htmlBuilder.AppendLine($"<canvas id='{canvasId}'></canvas>");
            htmlBuilder.AppendLine($"<script>");
            htmlBuilder.AppendLine($"document.addEventListener('DOMContentLoaded', function() {{");
            htmlBuilder.AppendLine($"var chartData = {chartData};");
            htmlBuilder.AppendLine($"var container = document.getElementById('{canvasId}');");
            htmlBuilder.AppendLine("showChart(container,chartData.upData,chartData.downData, chartData.cpuData, chartData.gpuData,chartData.taskData, chartData.labels);");
            htmlBuilder.AppendLine("});");
            htmlBuilder.AppendLine("</script>");
            htmlBuilder.AppendLine(Title(nod, 0, 1));
            htmlBuilder.AppendLine("<div class='status'>");
            htmlBuilder.AppendLine($"<span class='status-circle {(Status(nod) ? "status-true" : "status-false")}'></span>"); 
            htmlBuilder.AppendLine("</div>");

            htmlBuilder.AppendLine("</div>"); // close product
            

        }
        htmlBuilder.AppendLine("</div>");

        htmlBuilder.AppendLine("</body></html>");


        if (type == 0) File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Nodes.html"), htmlBuilder.ToString());
        if (type == 1) File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "NodesByHour.html"), htmlBuilder.ToString());
        if (type == 2) File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "NodesAllTime.html"), htmlBuilder.ToString());
        DeleteOldFilesOfData();

    }
    static void DeleteOldFilesOfData()
    {
        string directoryPath = Paths.NodeStatsDirectory; // Replace with your directory path
        TimeSpan fileAgeLimit = TimeSpan.FromDays(7); // Files older than one week

        // Get all files in the directory
        string[] files = Directory.GetFiles(directoryPath);

        // Current server time
        DateTime currentTime = DateTime.Now;

        foreach (string file in files)
        {
            FileInfo fileInfo = new FileInfo(file);

            // Calculate the age of the file
            TimeSpan fileAge = currentTime - fileInfo.LastWriteTime;

            // If the file is older than the specified limit, delete it
            if (fileAge > fileAgeLimit)
            {
                fileInfo.Delete();
                Console.WriteLine($"Deleted file: {fileInfo.Name}");
            }
        }
    }
    public static string GenerateChartData(NodeStats node)
    {
        // Сортировка ключей для обеспечения правильной последовательности времени на графике
        var sortedPartials = node.Partial.OrderBy(p => p.Key);

        // Взять последние 30 записей
        var lastPartials = sortedPartials.TakeLast(30);

        List<string> labels = new List<string>();
        List<double> cpuData = new List<double>();
        List<double> upData = new List<double>();
        List<double> downData = new List<double>();
        List<double> gpuData = new List<double>();
        List<int> taskData = new List<int>();

        foreach (var partial in lastPartials)
        {
            DateTime time = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(partial.Key)).LocalDateTime;
            labels.Add(time.ToString("yyyy-MM-dd HH:mm:ss"));
            cpuData.Add(Math.Round(100 * partial.Value.CpuLoad));
            gpuData.Add(Math.Round(100 * partial.Value.GpuLoad));
            upData.Add( partial.Value.InternetUp*0.0001f);
            downData.Add( partial.Value.InternetDown*0.0001f);
        }

        // Подготовка словаря для суммирования задач
        var tasksPerLabel = labels.ToDictionary(label => label, label => 0);

        // Агрегация данных о задачах
        if (node.completedTasks != null)
        {
            foreach (var task in node.completedTasks)
            {
                // Нахождение ближайшей метки времени
                string closestLabel = FindClosestLabel(labels, task.Time);
                if (closestLabel != null)
                {
                    tasksPerLabel[closestLabel] += 1;
                }
            }
        }

        // Перенос сумм задач в список для графика
        taskData.AddRange(tasksPerLabel.Values);

        var jsonData = JsonConvert.SerializeObject(new { labels, cpuData, gpuData, taskData,upData,downData });

        return jsonData;
    }
    private static string FindClosestLabel(List<string> labels, DateTime target)
    {
        // Преобразование строковых меток обратно в DateTime для сравнения
        var labelDates = labels.Select(DateTime.Parse).ToList();
        DateTime closest = labelDates.Aggregate((x, y) => Math.Abs((x - target).Ticks) < Math.Abs((y - target).Ticks) ? x : y);

        return closest.ToString("yyyy-MM-dd HH:mm:ss");
    }
    public static string GenerateAveragedByHourChartData(NodeStats node)
    {
        var sortedPartials = node.Partial.OrderBy(p => p.Key).ToList();

        // Если данных меньше 2 точек, вернуть пустые данные
        if (sortedPartials.Count < 2)
            return JsonConvert.SerializeObject(new { labels = new string[0], cpuData = new double[0], gpuData = new double[0], taskData = new int[0] });

        // Определение временных границ (последние 65 минут)
        var endTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(sortedPartials.Last().Key)).LocalDateTime;
        var startTime = endTime.AddMinutes(-65);

        // Выбор соответствующих данных в временном диапазоне
        var relevantPartials = sortedPartials.Where(p =>
        {
            var time = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(p.Key)).LocalDateTime;
            return time >= startTime && time <= endTime;
        }).ToList();

        // Группировка данных по временным интервалам (по 2-минутным интервалам)
        var groupedPartials = relevantPartials.GroupBy(p =>
        {
            var time = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(p.Key)).LocalDateTime;
            return new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute / 2 * 2, 0); // Группировка по 2-минутным интервалам
        }).ToList();

        // Подготовка словаря для суммирования задач
        var tasksPerInterval = groupedPartials.ToDictionary(
            g => g.Key,
            g => 0 // Инициализация количества задач в этом интервале
        );

        // Агрегация данных о задачах
        if (node.completedTasks != null)
        {
            foreach (var task in node.completedTasks)
            {
                var taskTime = task.Time;
                // Нахождение ближайшего интервала для каждой задачи
                var closestInterval = groupedPartials
                    .Where(g => g.Key <= taskTime && g.Key.AddMinutes(2) > taskTime)
                    .Select(g => g.Key)
                    .FirstOrDefault();

                if (closestInterval != default)
                {
                    tasksPerInterval[closestInterval] += 1; // Инкремент количества задач
                }
            }
        }

        // Усреднение данных в каждом интервале
        var averagedPartials = groupedPartials.Select(g =>
        {
            var time = g.Key.ToString("yyyy-MM-dd HH:mm:ss");
            var cpuLoadAverage = g.Average(p => p.Value.CpuLoad) * 100;
            var gpuLoadAverage = g.Average(p => p.Value.GpuLoad) * 100;
            var taskCount = tasksPerInterval[g.Key]; // Получение суммы задач для этого интервала
            return new { time, cpuLoadAverage, gpuLoadAverage, taskCount };
        }).ToList();

        // Формирование JSON-объекта
        var jsonData = JsonConvert.SerializeObject(new
        {
            labels = averagedPartials.Select(p => p.time),
            cpuData = averagedPartials.Select(p => p.cpuLoadAverage),
            gpuData = averagedPartials.Select(p => p.gpuLoadAverage),
            taskData = averagedPartials.Select(p => p.taskCount) // Добавляем данные о задачах
        });

        return jsonData;
    }
    public static string GenerateAveragedByHourChartDataOld(NodeStats node)
    {
        var sortedPartials = node.Partial.OrderBy(p => p.Key).ToList();

        // Если данных меньше 2 точек, вернуть пустые данные
        if (sortedPartials.Count < 2)
            return JsonConvert.SerializeObject(new { labels = new string[0], cpuData = new double[0], gpuData = new double[0] });

        // Определение временных границ (последние 65 минут)
        var endTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(sortedPartials.Last().Key)).LocalDateTime;
        var startTime = endTime.AddMinutes(-65);

        // Выбор соответствующих данных в временном диапазоне
        var relevantPartials = sortedPartials.Where(p =>
        {
            var time = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(p.Key)).LocalDateTime;
            return time >= startTime && time <= endTime;
        }).ToList();

        // Группировка данных по временным интервалам (по 65/30 минут)
        var groupedPartials = relevantPartials.GroupBy(p =>
        {
            var time = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(p.Key)).LocalDateTime;
            return new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute / 2 * 2, 0); // Группировка по 2-минутным интервалам
        }).ToList();

        // Усреднение данных в каждом интервале
        var averagedPartials = groupedPartials.Select(g =>
        {
            var time = g.Key.ToString("yyyy-MM-dd HH:mm:ss");
            var cpuLoadAverage = g.Average(p => p.Value.CpuLoad) * 100;
            var gpuLoadAverage = g.Average(p => p.Value.GpuLoad) * 100;
            return new { time, cpuLoadAverage, gpuLoadAverage };
        }).ToList();

        // Формирование JSON-объекта
        var jsonData = JsonConvert.SerializeObject(new
        {
            labels = averagedPartials.Select(p => p.time),
            cpuData = averagedPartials.Select(p => p.cpuLoadAverage),
            gpuData = averagedPartials.Select(p => p.gpuLoadAverage)
        });

        return jsonData;
    }
    public static string GenerateAveragedAllTimeChartData(NodeStats node)
    {
        var sortedPartials = node.Partial.OrderBy(p => p.Key).ToList();

        // Return empty data if less than 2 data points are available
        if (sortedPartials.Count < 2)
            return JsonConvert.SerializeObject(new { labels = new string[0], cpuData = new double[0], gpuData = new double[0], taskData = new int[0] });

        // Determine the time span of the data
        var firstDate = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(sortedPartials.First().Key)).DateTime;
        var lastDate = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(sortedPartials.Last().Key)).DateTime;
        var totalDuration = lastDate - firstDate;

        // Calculate intervals for 30 points
        var interval = totalDuration.TotalSeconds / 29;

        // Create arrays for interpolated results
        List<string> labels = new List<string>();
        List<double> cpuData = new List<double>();
        List<double> gpuData = new List<double>();
        List<int> taskData = new List<int>();  // Add task data array

        // Assuming node.completedTasks is a list of tasks with Time and other properties
        if (node.completedTasks != null)
        {
            // Aggregate tasks by intervals similarly to cpuData and gpuData
            var groupedTasks = node.completedTasks.GroupBy(t =>
                firstDate.AddSeconds((int)((t.Time - firstDate).TotalSeconds / interval) * interval))
                .ToDictionary(g => g.Key, g => g.Count());

            // Fill taskData for each interval
            for (int i = 0; i <= 29; i++)
            {
                var currentTime = firstDate.AddSeconds(interval * i);
                taskData.Add(groupedTasks.ContainsKey(currentTime) ? groupedTasks[currentTime] : 0);
            }
        }

        // Generate 30 interpolated data points
        for (int i = 0; i < 30; i++)
        {
            var currentTime = firstDate.AddSeconds(interval * i);
            labels.Add(currentTime.ToString("yyyy-MM-dd HH:mm:ss"));

            // Find the two nearest data points for interpolation
            var before = sortedPartials.LastOrDefault(p => DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(p.Key)).DateTime <= currentTime);
            var after = sortedPartials.FirstOrDefault(p => DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(p.Key)).DateTime >= currentTime);

           
                var beforeTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(before.Key)).DateTime;
                var afterTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(after.Key)).DateTime;
                var timeFactor = (currentTime - beforeTime).TotalSeconds / (afterTime - beforeTime).TotalSeconds;

                // Linear interpolation for CPU and GPU load
                var interpolatedCpu = before.Value.CpuLoad + (after.Value.CpuLoad - before.Value.CpuLoad) * timeFactor;
                var interpolatedGpu = before.Value.GpuLoad + (after.Value.GpuLoad - before.Value.GpuLoad) * timeFactor;

                cpuData.Add(interpolatedCpu * 100);
                gpuData.Add(interpolatedGpu * 100);
             
        }

        // Create JSON object
        var jsonData = JsonConvert.SerializeObject(new
        {
            labels = labels,
            cpuData = cpuData,
            gpuData = gpuData,
            taskData = taskData // Add taskData to the output JSON
        });

        return jsonData;
    }

    public static string GenerateAveragedAllTimeChartDataOld(NodeStats node)
    {
        var sortedPartials = node.Partial.OrderBy(p => p.Key).ToList();

        // Return empty data if less than 2 data points are available
        if (sortedPartials.Count < 2)
            return JsonConvert.SerializeObject(new { labels = new string[0], cpuData = new double[0], gpuData = new double[0] });

        // Determine the time span of the data
        var firstDate = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(sortedPartials.First().Key)).DateTime;
        var lastDate = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(sortedPartials.Last().Key)).DateTime;
        var totalDuration = lastDate - firstDate;

        // Calculate intervals for 30 points
        var interval = totalDuration.TotalSeconds / 29;

        // Create arrays for interpolated results
        List<string> labels = new List<string>();
        List<double> cpuData = new List<double>();
        List<double> gpuData = new List<double>();

        // Generate 30 interpolated data points
        for (int i = 0; i < 30; i++)
        {
            var currentTime = firstDate.AddSeconds(interval * i);
            labels.Add(currentTime.ToString("yyyy-MM-dd HH:mm:ss"));

            // Find the two nearest data points for interpolation
            var before = sortedPartials.LastOrDefault(p => DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(p.Key)).DateTime <= currentTime);
            var after = sortedPartials.FirstOrDefault(p => DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(p.Key)).DateTime >= currentTime);

            if (before.Key == after.Key) // Only one point or exact match
            {
                cpuData.Add(before.Value.CpuLoad * 100);
                gpuData.Add(before.Value.GpuLoad * 100);
            }
            else
            {
                var beforeTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(before.Key)).DateTime;
                var afterTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(after.Key)).DateTime;
                var timeFactor = (currentTime - beforeTime).TotalSeconds / (afterTime - beforeTime).TotalSeconds;

                // Linear interpolation for CPU and GPU load
                var interpolatedCpu = before.Value.CpuLoad + (after.Value.CpuLoad - before.Value.CpuLoad) * timeFactor;
                var interpolatedGpu = before.Value.GpuLoad + (after.Value.GpuLoad - before.Value.GpuLoad) * timeFactor;

                cpuData.Add(interpolatedCpu * 100);
                gpuData.Add(interpolatedGpu * 100);
            }
        }

        // Create JSON object
        var jsonData = JsonConvert.SerializeObject(new
        {
            labels = labels,
            cpuData = cpuData,
            gpuData = gpuData
        });

        return jsonData;
    }
    public static bool Status(NodeStats node)
    {
        var sortedPartials = node.Partial.OrderBy(p => p.Key).ToList();

        // Если данных меньше двух, невозможно определить разрыв
        if (sortedPartials.Count < 2)
            return true;

        // Перебор отсортированных данных для проверки промежутков
        for (int i = 1; i < sortedPartials.Count; i++)
        {
            long previousTimestamp = long.Parse(sortedPartials[i - 1].Key);
            long currentTimestamp = long.Parse(sortedPartials[i].Key);

            // Проверка разрыва более чем на один час (3600000 миллисекунд)
            if (currentTimestamp - previousTimestamp > 20*60000)
            {
                return false;
            }
        }

        // Если разрывов более одного часа не найдено
        return true;
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

       
        string Link = $"<div class='title' style='background: linear-gradient(to right, {colorB}, {colorA}); color: white; text-decoration: none;'><p><a href='hh' style='color: white; text-decoration: none;'>{Title}</a></p></div>";

        // Возвращаем сформированную строку с HTML
        return Link;
    }
   
    public static string GenerateChartData2(NodeStats node)
    {
        // Предполагается, что данные уже отсортированы по времени
        var allData = node.Partial.OrderBy(p => p.Key).ToList();

        if (!allData.Any())
            return JsonConvert.SerializeObject(new { labels = new string[0], cpuData = new double[0], gpuData = new double[0] });

        // Определяем начальное и конечное время
        var startTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(allData.First().Key)).LocalDateTime;
        var endTime = startTime.AddDays(1); // Заканчиваем на следующие сутки от начала

        // Генерация 24 точек на оси времени
        var timeIntervals = Enumerable.Range(0, 24).Select(i => startTime.AddHours(i)).ToList();

        List<string> labels = new List<string>();
        List<double> cpuData = new List<double>();
        List<double> gpuData = new List<double>();

        foreach (var time in timeIntervals)
        {
            labels.Add(time.ToString("yyyy-MM-dd HH:mm:ss"));

            // Находим все записи, которые попадают в текущий интервал времени
            var relevantPartials = allData.Where(p =>
                DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(p.Key)).LocalDateTime >= time &&
                DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(p.Key)).LocalDateTime < time.AddHours(1)).ToList();

            // Вычисляем среднее значение для CPU и GPU
            cpuData.Add(relevantPartials.Any() ? relevantPartials.Average(p => 100 * p.Value.CpuLoad) : 0);
            gpuData.Add(relevantPartials.Any() ? relevantPartials.Average(p => 100 * p.Value.GpuLoad) : 0);
        }

        var jsonData = JsonConvert.SerializeObject(new { labels, cpuData, gpuData });
        return jsonData;
    }
}
