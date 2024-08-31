using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
public static class GPT4AllAPIClient
{
    private static readonly HttpClient client = new HttpClient();

    /// <summary>
    /// Sends a request to the GPT4All API and gets the response.
    /// </summary>
    /// <param name="prompt">The prompt to send to the GPT4All API.</param>
    /// <returns>The response from the GPT4All API.</returns>
    public static async Task<string> GetResponseFromGPT4All(string prompt)
    {
        try
        {
            string url = "http://localhost:4892/v1/completions";
            var requestBody = new
            {
                model = "mistral-7b-instruct-v0.2-GGUF",
                prompt = prompt,
                temperature = 0.7,
                max_tokens = 1200,
                top_p = 0.95,
                n = 1,
                stream = false
            };

            var jsonContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Content = jsonContent;

                HttpResponseMessage response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
        }
        catch (HttpRequestException e)
        {
            Logger.AddLog($"Request error: {e.Message}");
            return null;
        }
        catch (Exception e)
        {
            Logger.AddLog($"General error: {e.Message}");
            return null;
        }
    } 

}