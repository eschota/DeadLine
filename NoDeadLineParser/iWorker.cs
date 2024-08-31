using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



public static class BD
{
    public static List<iWorker> workers = new List<iWorker>();
    public static List<iTask> AllTasks = new List<iTask>();
    public static List<int> Ids = new List<int>();

    private static int _currentID = 0;
    public static int CurrentID => _currentID++;
    
    public static void LoadWorkers()
    {
        try
        {
            foreach (var item in System.IO.Directory.GetFiles(Paths.ExtestionWorkersPath))
            {
                workers.Add(JsonConvert.DeserializeObject<iWorker>(System.IO.File.ReadAllText(item)));
            }
            LoadiTasks();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        
    }    public static void LoadiTasks()
    {
        try
        {
            foreach (var item in System.IO.Directory.GetFiles(Paths.ExtestioniTasksPath))
            {
                AllTasks.Add(JsonConvert.DeserializeObject<iTask>(System.IO.File.ReadAllText(item)));
            }

            _currentID=AllTasks.Count-1;
            if (_currentID < 0) _currentID = 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        
    }
}
public class iWorker
{

    public int installId { get; set;}
    public string nick { get; set; }
    public DateTime startActivity { get; set; }
    public DateTime lastActivity { get; set; }
    public List<iTask> tasksDone { get; set; }




    public iTask SignUp()
    {
       
            int uuid = new Random().Next(1000000, 9999999);
            while (BD.workers.Exists(X => X.installId == uuid))
            {
                uuid = new Random().Next(1000000, 9999999);
            }
        
            
            
            Console.WriteLine(nick + " UUID: " + uuid);
            BD.workers.Add(this);
            installId=uuid;
            Save();
            return new iTask(iTask.TaskType.SIGNUP, uuid, nick, "", 50);
    }
    public List<iTask> Update(iTask itask)
    {
        if (Program.TS.LocalPages.Count == 0) Program.TS.FindPagesToParse();

            if (Program.TS.LocalPages.Count>0)
        {
           

            List<iTask> tasks = new List<iTask>();
            tasks.Add(new iTask(iTask.TaskType.PARSE, installId, nick, Program.TS.LocalPages[new Random().Next(0, Program.TS.LocalPages.Count)].url, 1000));
            tasks.Add(new iTask(iTask.TaskType.PARSE, installId, nick, Program.TS.LocalPages[new Random().Next(0, Program.TS.LocalPages.Count)].url, 5000));
            tasks.Add(new iTask(iTask.TaskType.PARSE, installId, nick, Program.TS.LocalPages[new Random().Next(0, Program.TS.LocalPages.Count)].url, 2000));
            tasks.Add(new iTask(iTask.TaskType.PARSE, installId, nick, Program.TS.LocalPages[new Random().Next(0, Program.TS.LocalPages.Count)].url, 1000));
            tasks.Add(new iTask(iTask.TaskType.PARSE, installId, nick, Program.TS.LocalPages[new Random().Next(0, Program.TS.LocalPages.Count)].url, 2500));
            tasks.Add(new iTask(iTask.TaskType.PARSE, installId, nick, Program.TS.LocalPages[new Random().Next(0, Program.TS.LocalPages.Count)].url, 3500));
            tasks.Add(new iTask(iTask.TaskType.PARSE, installId, nick, Program.TS.LocalPages[new Random().Next(0, Program.TS.LocalPages.Count)].url, 1500));
            tasks.Add(new iTask(iTask.TaskType.PARSE, installId, nick, Program.TS.LocalPages[new Random().Next(0, Program.TS.LocalPages.Count)].url, 1700));
            TGBot.BotSendText(-4152887032, "Tasks Created!");

            return tasks;
        }
        else
        {
            List<iTask> tasks = new List<iTask>();
            TGBot.BotSendText(-4152887032, "No pages to parse");
            tasks.Add(new iTask(iTask.TaskType.UPDATE, installId, nick, "", 30000));
            return tasks;
        }
    }

    private void Save()
    {
        try
        {
            File.WriteAllText(Path.Combine(Paths.ExtestionWorkersPath, $"_{installId}.json"), JsonConvert.SerializeObject(this));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());   
        }
    }
}