using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
internal class Injesters
{
    static int last = 0;
    static int lastUsers = 0;
    public static async Task ParseIngesters(string html)
    {
        string pattern = @"(\d+) files total queued since";
        Regex regex = new Regex(pattern);

        int totalFilesQueued = 0;

        // Ищем все совпадения
        MatchCollection matches = regex.Matches(html);
        int userCount= 0;
        foreach (Match match in matches)
        {
            // Извлекаем число из найденной строки и суммируем
            if (int.TryParse(match.Groups[1].Value, out int filesQueued))
            {
                userCount++;
                totalFilesQueued += filesQueued;
            }
        }

        Console.WriteLine("totalFilesQueued: "+ totalFilesQueued);
      
        if (userCount>5 && totalFilesQueued > 100) TGBot.BotSendText(-1001413825569, "Ingestion Queue: " + totalFilesQueued + $" [{totalFilesQueued-last}] Users: {userCount} [{userCount-lastUsers}]");
        last = totalFilesQueued;
        lastUsers = userCount;
        //TGBot.SetBotCustomTitle(-1001413825569, "Ingestion Queue: " + totalFilesQueued);
    }
    public static async Task ParseAdminApprove(string html)
    {
        // Проверяем полученный HTML
        Console.WriteLine("Received HTML: " + html);

        // Регулярное выражение для поиска значения внутри <span class='value'>
        string pattern = @"<span class=[""']value[""']>(\d+)</span>";
        Regex regex = new Regex(pattern);

        int totalUsersForApprove = 0;

        // Ищем все совпадения
        MatchCollection matches = regex.Matches(html);

        foreach (Match match in matches)
        {
            // Извлекаем число из найденной строки
            if (int.TryParse(match.Groups[1].Value, out int usersForApprove))
            {
                totalUsersForApprove += usersForApprove;
            }
        }

        Console.WriteLine("totalUsersForApprove: " + totalUsersForApprove);

        if (totalUsersForApprove > 0)
        {
            TGBot.BotSendText(-1001413825569, "На Кверти ожидают аппрува документов: " + totalUsersForApprove);
        }
    }
} 