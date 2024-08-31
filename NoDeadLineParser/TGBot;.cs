using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using static System.Runtime.InteropServices.JavaScript.JSType;


internal class TGBot
{
    internal static TelegramBotClient Bot;
    internal static Message LastMessage;
    internal static Message LastMessageApprove;

    public static void StartBot()
    {
        Bot = new TelegramBotClient("7198960639:AAEz2yNiiSF-vvi1l0QYTezDbcUEMR3MpgY"); 
    }
    public static async Task BotSendText(long chatID, string txt)
    {
        await Task.Delay(333);
        txt += $"[{DateTime.UtcNow}] ";
        try
        {
            if (LastMessage != null && LastMessage.Chat.Id == chatID)
            {
                try
                {
                    // Пытаемся отредактировать последнее сообщение
                    LastMessage = await Bot.EditMessageTextAsync(chatID, LastMessage.MessageId, txt);
                }
                catch (Exception ex) when (ex.Message.Contains("message is not modified") || ex.Message.Contains("message to edit not found"))
                {
                    // Если редактирование невозможно, отправляем новое сообщение
                    LastMessage = await Bot.SendTextMessageAsync(chatID, txt, disableNotification: true);
                }
            }
            else
            {
                // Если нет предыдущего сообщения, отправляем новое
                LastMessage = await Bot.SendTextMessageAsync(chatID, txt, disableNotification: true);
            }
        }
        catch
        {
            // Логируем ошибку или обрабатываем исключение
        }
    }

    public static async Task BotSendApprove(long chatID, string txt)
    {
        if (LastMessageApprove != null) await Bot.DeleteMessageAsync(LastMessageApprove.Chat.Id, LastMessageApprove.MessageId);
        LastMessageApprove = await Bot.SendTextMessageAsync(chatID,txt, disableNotification: true);


        
      

    }
    public static async Task SetCustomTitle(long chatID, long userID, string customTitle)
    {
        try
        {
            await Bot.SetChatAdministratorCustomTitleAsync(
                chatId: chatID,
                userId: userID,
                customTitle: customTitle
           
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting custom title: {ex.Message}");
        }
    }

    public static async Task SetBotCustomTitle(long chatID, string customTitle)
    {
        // Получение информации о боте, чтобы узнать его userID
        var botUser = await Bot.GetMeAsync();
        await SetCustomTitle(chatID, botUser.Id, customTitle);



    }
} 