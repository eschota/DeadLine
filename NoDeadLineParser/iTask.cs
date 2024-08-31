using Newtonsoft.Json;
using OpenAIClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
 
    public class iTask
    {
    public enum TaskType
    { 
        SIGNUP,
        UPDATE,
        PARSE,        
        RUN_SCRIPT
    }
     
    public TaskType type;
    public string login;
    
    public int taskId;
    public int installId;
    public string nick;
    public string url;
    public int delay;
    public string result;
    public string[] scripts;

    public TaskResult taskResult;
     
    public class TaskResult
    {
        public string html;

     
    }
    [JsonProperty("runningtasks")]
    public int[] runningTasks;
    public iTask(TaskType _type,int _installID, string _nick, string _url, int _delay)
    {
        installId = _installID;
        type = _type;        
        taskId = BD.CurrentID;
        nick = _nick;
        url = _url;
        delay = _delay;  
        BD.AllTasks.Add(this);
        Save();
    }
    public iTask()
    {
        BD.AllTasks.Add(this);
    }
    private void Save()
    {
        try
        {
            File.WriteAllText(Path.Combine(Paths.ExtestioniTasksPath, $"_{taskId}.json"), JsonConvert.SerializeObject(this));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

} 
