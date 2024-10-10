using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;

 
    public static class Answer
    {
        public static async Task <bool> CognitiveAnswer(Message message)
        {
        var assistantFunctions = await OpenAIClient.AskOpenAI_Activate_Functions(message.Text);

        if(assistantFunctions.Count==0) await Chat.Bot.SendTextMessageAsync((int)message.Chat.Id, "функция не распознана");
        foreach (var assistantFunction in assistantFunctions)
        {
            if (assistantFunction.FunctionName == "GenerateImages")
            {
                int count = Convert.ToInt32(assistantFunction.Parameters["count"]);
                var imagePrompts = ((JArray)assistantFunction.Parameters["imagePrompts"]).ToObject<List<string>>();

                await Chat.Bot.SendTextMessageAsync((int)message.Chat.Id, "Типа пиздатые картинки по запросу");
                // Вызов вашей функции генерации изображений
                //await GenerateImages(count, imagePrompts, chatid);
            }
            else if (assistantFunction.FunctionName == "JustAnswer")
            {
                string promptResponse = assistantFunction.Parameters["prompt"].ToString();
                await Chat.Bot.SendTextMessageAsync((int)message.Chat.Id, promptResponse);
            }
        }
        return true;
        }
    } 