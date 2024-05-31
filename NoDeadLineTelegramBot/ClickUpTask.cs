using Newtonsoft.Json;
using System.Text;

public static class Logger
{
    public static void AddLog(string logMessage)
    {
        // Implementation for logging
        Console.WriteLine(logMessage);
    }
}

public class ClickUpTask
{
    public string Name { get; set; }
    public string Description { get; set; }
}

public class ClickUpTaskResponse
{
    public string Id { get; set; }
    public string Url { get; set; }
}

public class ClickUpApi
{
    private readonly string _apiKey;
    private readonly string _teamId;
    private readonly string _spaceId;
    private readonly string _listId;
    private readonly HttpClient _httpClient;

    public ClickUpApi(string apiKey, string teamId, string spaceId, string listId)
    {
        _apiKey = apiKey;
        _teamId = teamId;
        _spaceId = spaceId;
        _listId = listId;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", _apiKey);
    }

    /// <summary>
    /// Creates a new task in ClickUp and returns its details including the URL.
    /// </summary>
    /// <param name="taskName">The name of the task.</param>
    /// <param name="taskDescription">The description of the task.</param>
    /// <returns>The created task's details including the URL as a string.</returns>
    public async Task<string> CreateTaskAsync(string taskName, string taskDescription)
    {
        var task = new ClickUpTask
        {
            Name = taskName,
            Description = taskDescription
        };

        var json = JsonConvert.SerializeObject(task);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var url = $"https://api.clickup.com/api/v2/list/{_listId}/task";

        var response = await _httpClient.PostAsync(url, content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var createdTask = JsonConvert.DeserializeObject<ClickUpTaskResponse>(responseContent);
            Logger.AddLog($"Task created successfully: {createdTask.Url}");
            return $"Task created successfully. URL: {createdTask.Url}";
        }
        else
        {
            Logger.AddLog($"Error creating task: {responseContent}");
            throw new Exception(responseContent);
        }
    }
}