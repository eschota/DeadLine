using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using OpenAIClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

public class Product
{
    public string Div = "";
    public int ProductID = -1;
    public string ProductName = "";
    [JsonProperty("Url")]
    public string url = "";
    public string ProductMainPreviewUrl = "";
    public List<float> Price = new List<float>();
    public List<int> Pos = new List<int> ();
    [JsonProperty("ProductDate")]
    public List<DateTime> ProductDate = new List<DateTime> { };
    [JsonProperty("Author")]
    public string ProductAuthor = "";
    public DateTime SubmitDate = new DateTime(1, 1, 1);
    public List<string> Formats = new List<string>();
    public enum cert {NotParsed=-1,No=0,Lite=1,Pro=2,steamCell=3 };
    public cert Certificate = cert.NotParsed;
    public string AuthorLink = "";
    public string Description = "";
    public List<string> Tags = new List<string>();

    public static Product Load(string filePath)
    {
        try 
        {
        //UpdateCertificateInJson(filePath, -1);
        var json = File.ReadAllText(filePath); 
        Product p = JsonConvert.DeserializeObject<Product>(json);

        return p;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        return null;
    }
    public static void UpdateCertificateInJson(string filePath, int newCertificateValue)
    {
        // Read the existing JSON file
        var json = File.ReadAllText(filePath);

        // Deserialize the JSON to a dynamic object to allow modification
        Product jsonObject = JsonConvert.DeserializeObject<Product>(json);
        var uniqueDates = jsonObject.ProductDate
            .Select(date => new DateTime(date.Year, date.Month, date.Day)) // Преобразование в DateTime с игнорированием времени
            .Distinct() // Удаление дубликатов
            .ToList();

        // Обновляем список дат в объекте
        jsonObject.ProductDate = uniqueDates;

        jsonObject.ProductDate.RemoveAll(x => x.Year == new DateTime(1, 1, 1).Year);
       

        // Serialize the updated JSON back to a string
        string updatedJson = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);

        // Write the updated JSON back to the file
        File.WriteAllText(filePath, updatedJson);

        // Since logging is removed as per the updated request, omit the log call
        // Logger.AddLog("Updated the certificate in JSON file and removed duplicates.");
    }
    public async Task Save(string filePath)
    { 
        int attempts = 0;

        while (attempts < 3)
        {
            try
            {
                File.WriteAllText(filePath, JsonConvert.SerializeObject(this));
                break; 
            }
            catch (Exception e) when (e is UnauthorizedAccessException || e is DirectoryNotFoundException || e is IOException)
            {
                attempts++;
                Logger.AddLog($"Attempt {attempts} failed when saving to {filePath}. Error: {e.Message}");

                if (attempts >= 3) 
                { 
                    Logger.AddLog($"Failed to save to {filePath} after {3} attempts.");
                    break;
                }

                // Wait a bit before retrying (500 milliseconds here)
                await Task.Delay(1000);
            }
        }
    }
    public class CustomDateTimeConverter : IsoDateTimeConverter
    {
        public CustomDateTimeConverter()
        {
            base.DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffffffK";
        }
    }
}