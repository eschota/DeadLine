using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OpenAIClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

public class Product
{
    public int ProductID = -1;
    public string ProductName = "";
    [JsonProperty("Url")]
    public string url = "";
    public string ProductMainPreviewUrl = "";
    public List<float> Price = new List<float>();
    public List<int> Pos = new List<int> ();
    [JsonProperty("ProductDate")]
    public List<DateTime> ProductDate = new List<DateTime> { new DateTime(1,1,1)};
    [JsonProperty("Author")]
    public string ProductAuthor = "";
    public DateTime SubmitDate = new DateTime(1, 1, 1);
    public List<string> Formats = new List<string>();
    public string Certificate = "";
    public string AuthorLink = "";
    public string Description = "";
    public List<string> Tags = new List<string>();

    public static Product Load(string filePath)
    {

        var json = File.ReadAllText(filePath);
        Product p = JsonConvert.DeserializeObject<Product>(json);
        return p;
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