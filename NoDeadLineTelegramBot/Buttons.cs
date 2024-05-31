
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
public static class Buttons
{
  

    /// <summary>
    /// Handles the callback query from the inline button.
    /// </summary>
    /// <param name="callbackQuery">The callback query.</param>
    public static async Task HandleCallbackQuery(Update up)
    {
        try
        {
            //    await Chat.Bot.AnswerCallbackQueryAsync(
            //    callbackQueryId: up.CallbackQuery.Id,
            //    text: $"You clicked: {up.CallbackQuery.Data}"
            //);

            //    // Optionally, send a follow-up message
            //    await Chat.Bot.SendTextMessageAsync(
            //        chatId: up.CallbackQuery.Message.Chat.Id,
            //        text: $"Button clicked: {up.CallbackQuery.Data}"
            //    );

            int k = 0;
            int.TryParse(up.CallbackQuery.Data, out k);
            string fName = $"{DateTime.Now:yyyyMMdd_HHmmssfff}_rebuilded.png";
            string filePath = Path.Combine(Paths.Imagine, fName);

            string add = "";
            if (k == 4) add = " NSFW, NoWear, Boobs, Ass";
            if (k == 3) add = " NSFW, NoWear, Boobs, Ass";
            if (k == 2) add = " NoWear, Boobs, Ass";
            string mod = " [Real]";
            if (k == 4) mod = " [Pron2]";
            if (k == 3) mod = " [Pron1]";
            if (k == 2) mod = " [Anime]";
            string promt = up.CallbackQuery.Message.Caption.Split('\n')[1];
            mod = up.CallbackQuery.Message.Caption.Split('\n')[0] + mod;
            await StableDiffusion.StableDiffusionTxtToImage((promt + add), filePath, k);

            await Chat.SendPhotoMessage(up.CallbackQuery.Message.Chat.Id, filePath, mod+"\n"+promt, "", "");
        } catch (Exception e) { Console.WriteLine(e); }
       
    }

    // Example usage in your bot's update handler
    public static async Task HandleUpdateAsync(Update update)
    {
        if (update.Type == UpdateType.CallbackQuery)
        {
            await HandleCallbackQuery(update);
        }
    }
}