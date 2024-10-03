using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

 
    internal class Ollama
    {
    private static readonly HttpClient client = new HttpClient();

    public static async Task<string> AskLLama(string _prompt)
    { 
        string url = "http://localhost:11434/api/generate";

        var payload = new
        {
            model = "llama3",
            prompt = _prompt,// +" .ОТВЕЧАЙ СТРОГО НА РУССКОМ ЯЗЫКЕ!!!",
            stream = false
             
        };

        var jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        try
        {
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(1);
                HttpResponseMessage response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var responseObject = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(jsonResponse);

                    return responseObject.response;
                } 
            }
        }
        catch (Exception ex) { Logger.AddLog($"Error details: " + ex.Message); }
        return "";
    } 
}
 