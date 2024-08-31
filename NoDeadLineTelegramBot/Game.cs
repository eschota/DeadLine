using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Newtonsoft.Json;

using Telegram.Bot.Types.ReplyMarkups;

public static class Games
{
    public static List<Game> AllGames = new List<Game>();
    public static List<string> Promts = new List<string>();



    public static bool GetGame(Message m)
    {   if(m.Text==null) return false;
        if(m.Text.ToLower().Contains("игра"))
        {
            CreateGame(m);
            return true;

        }




        foreach (var currentGame in AllGames.Where(X=>X.state!=Game.State.Ended))
        {
            foreach (var gm in currentGame.messages)
            {
                if (gm.MessageId == m.ReplyToMessage.MessageId) UpdateGame(currentGame);
            }
        }
        return false;
    }

    public static void UpdateGame(Game game)
    {
     
    }
    public static void CreateGame(Message m)
    {
        var game = new Game();
        game.Name = m.Text;
        game.chat_id = m.Chat.Id;
        game.state = Game.State.Created;
        AllGames.Add(game);
        game.Save();
    }



    public static void LoadAllGames()
    {
        var allGames = new List<Game>();
        var Files = System.IO.Directory.GetFiles(Paths.Games);
        foreach (var file in Files)
        {
            AllGames.Add(JsonConvert.DeserializeObject<Game>(System.IO.File.ReadAllText(file)));
        }
        Files = System.IO.Directory.GetFiles(Paths.GamesPromts);
        foreach (var file in Files)
        {
            Promts.Add(System.IO.File.ReadAllText(file));
        }
    }



}
public class Game
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public string Aim;

        public string Hero;
        public double chat_id { get; set; }
        
        public List<Message> messages = new List<Message>();

            
        public List<User> Users = new List<User>();
        public State state;
        public enum State
        {
            Created,
            Started,
            Ended
        }
        public Game() { }

        public void Save()
    {
        if (Name == "") return;
        System.IO.File.WriteAllText(Paths.Games+Name+".json",JsonConvert.SerializeObject(new Game()));

    }

        

    }

