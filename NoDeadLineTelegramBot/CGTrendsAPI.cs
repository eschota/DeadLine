using Telegram.Bot;
using Telegram.Bot.Types;  
using Telegram.Bot.Types.ReplyMarkups; 
using Newtonsoft.Json;
using System.Text;

public class CGTrendsAPI
{
    private static readonly Dictionary<long, UserSession> userSessions = new Dictionary<long, UserSession>();

    public static async Task<bool> CGTrendsAPIRequest(Message message)
    {
        try
        {
            string itext = message.Text ?? message.Caption ?? string.Empty;
            if (string.IsNullOrEmpty(itext)) return false;

            itext = itext.ToLower();

            if (!itext.StartsWith("/cgtrends") && !itext.StartsWith(".спекутвы")) return false;

            // Если существует предыдущая сессия, удаляем все сообщения
            if (userSessions.TryGetValue(message.Chat.Id, out UserSession previousSession))
            {
                // Удаление старых сообщений галереи
                foreach (var messageId in previousSession.MessageIds)
                {
                    try
                    {
                        await Chat.Bot.DeleteMessageAsync(message.Chat.Id, messageId);
                    }
                    catch (Exception ex)
                    {
                        Logger.AddLog($"Ошибка при удалении предыдущего сообщения галереи: {ex.Message}");
                    }
                }

                // Удаление старого сообщения с подписью
                if (previousSession.SignatureMessageId != 0)
                {
                    try
                    {
                        await Chat.Bot.DeleteMessageAsync(message.Chat.Id, previousSession.SignatureMessageId);
                    }
                    catch (Exception ex)
                    {
                        Logger.AddLog($"Ошибка при удалении предыдущего сообщения с подписью: {ex.Message}");
                    }
                }

                previousSession.MessageIds.Clear();
                previousSession.SignatureMessageId = 0;
            }

            // Send initial status message
            Message statusMessage = await Chat.Bot.SendTextMessageAsync(message.Chat.Id, "Запрос получен");

            // Get products from API
            List<Product> products = await RenderfinPostAsync(itext);

            if (products == null || products.Count == 0)
            {
                await Chat.Bot.EditMessageTextAsync(statusMessage.Chat.Id, statusMessage.MessageId, "По вашему запросу ничего не найдено.");
                await Task.Delay(5000);
                await Chat.Bot.DeleteMessageAsync(message.Chat.Id, statusMessage.MessageId);
                return false;
            }

            // Store user session data (новая сессия)
            userSessions[message.Chat.Id] = new UserSession
            {
                Products = products,
                CurrentPageIndex = 0
            };

            // Delete the status message
            await Chat.Bot.EditMessageTextAsync(statusMessage.Chat.Id, statusMessage.MessageId, "Отправляю результаты.");

            // Send the first page of products
            await SendGalleryAsync(message.Chat.Id, products, 0, 5);

            await Chat.Bot.DeleteMessageAsync(message.Chat.Id, statusMessage.MessageId);
        }
        catch (Exception ex)
        {
            Logger.AddLog($"Ошибка при обработке запроса: {ex.Message}");
        }
        return true;
    }


    public static async Task<List<Product>> RenderfinPostAsync(string prompt)
    {
        using (HttpClient client = new HttpClient())
        {
            var requestContent = new StringContent(
                JsonConvert.SerializeObject(new { prompt = prompt }),
                Encoding.UTF8,
                "application/json"
            );

            HttpResponseMessage response = await client.PostAsync("https://renderfin.com/api-trends-NLP", requestContent);

            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                List<Product> products = JsonConvert.DeserializeObject<List<Product>>(responseContent);
                return products;
            }
            else
            {
                // Handle error response
                return null;
            }
        }
    }

    public static async Task SendGalleryAsync(long chatId, List<Product> products, int pageIndex, int pageSize)
    {
        try
        {
            if (userSessions.TryGetValue(chatId, out UserSession session))
            {
                // Удаление старых сообщений галереи
                foreach (var messageId in session.MessageIds)
                {
                    try
                    {
                        await Chat.Bot.DeleteMessageAsync(chatId, messageId);
                    }
                    catch (Exception ex)
                    {
                        Logger.AddLog($"Ошибка при удалении сообщения галереи: {ex.Message}");
                    }
                }

                // Удаление старого сообщения с подписью
                if (session.SignatureMessageId != 0)
                {
                    try
                    {
                        await Chat.Bot.DeleteMessageAsync(chatId, session.SignatureMessageId);
                    }
                    catch (Exception ex)
                    {
                        Logger.AddLog($"Ошибка при удалении сообщения с подписью: {ex.Message}");
                    }
                }

                // Очистка списка сообщений после удаления
                session.MessageIds.Clear();
                session.SignatureMessageId = 0;
            }

            int totalPages = (int)Math.Ceiling((double)products.Count / pageSize);
            var items = products.Skip(pageIndex * pageSize).Take(pageSize).ToList();

            // Отправка галереи без подписи
            var sentMessages = await Chat.Bot.SendMediaGroupAsync(chatId, new IAlbumInputMedia[]
            {
            new InputMediaPhoto(InputFile.FromUri(items[0].product_url_preview)),
            new InputMediaPhoto(InputFile.FromUri(items[1].product_url_preview)),
            new InputMediaPhoto(InputFile.FromUri(items[2].product_url_preview)),
            new InputMediaPhoto(InputFile.FromUri(items[3].product_url_preview)),
            new InputMediaPhoto(InputFile.FromUri(items[4].product_url_preview))
            });

            UserSession sessionUpdated = null;

            // Проверка на null перед доступом к сообщениям
            if (sentMessages != null && userSessions.TryGetValue(chatId, out sessionUpdated))
            {
                sessionUpdated.MessageIds = sentMessages.Select(m => m.MessageId).ToList();
                sessionUpdated.TotalPages = totalPages;
                sessionUpdated.PageSize = pageSize;
                sessionUpdated.Products = products;
            }

            // Генерация подписи с помощью MarkdownV2
            string caption = GenerateCaption(products, pageIndex, pageSize);

            try
            {
                // Отправка отдельного сообщения с подписью
                var signatureMessage = await Chat.Bot.SendTextMessageAsync(
                    chatId,
                    caption,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.MarkdownV2
                );

                // Сохраняем идентификатор сообщения с подписью
                if (sessionUpdated != null)
                {
                    sessionUpdated.SignatureMessageId = signatureMessage.MessageId;
                }
            }
            catch (Exception ex)
            {
                Logger.AddLog($"Ошибка при отправке сообщения с подписью: {ex.Message}");
            }

            // Создание кнопок навигации с правильной CallbackData
            var buttons = new List<InlineKeyboardButton>();

            if (pageIndex > 0)
                buttons.Add(InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"page_{pageIndex - 1}"));
            if (pageIndex < totalPages - 1)
                buttons.Add(InlineKeyboardButton.WithCallbackData("➡️ Вперед", $"page_{pageIndex + 1}"));

            var inlineKeyboard = new InlineKeyboardMarkup(buttons);

            // Отправка сообщения навигации
            var navigationMessage = await Chat.Bot.SendTextMessageAsync(
                chatId,
                $"Страница {pageIndex + 1} из {totalPages}",
                replyMarkup: inlineKeyboard
            );

            // Хранение идентификатора сообщения навигации
            if (sessionUpdated != null)
            {
                sessionUpdated.MessageIds.Add(navigationMessage.MessageId);
                sessionUpdated.CurrentPageIndex = pageIndex;  // Обновляем индекс текущей страницы
            }
        }
        catch (Exception ex)
        {
            Logger.AddLog(ex.Message);
        }
    }



    // Отдельная функция для генерации caption с MarkdownV2
    public static string GenerateCaption(List<Product> products, int pageIndex, int pageSize)
    {
        var items = products.Skip(pageIndex * pageSize).Take(pageSize).ToList();

        StringBuilder captionBuilder = new StringBuilder();
        captionBuilder.AppendLine($"*Найдено*: *{products.Count}* продуктов  по позициям в топе\\.\n");

        for (int i = 0; i < items.Count; i++)
        {
            var product = items[i];

            // Количество дней с момента добавления в базу данных
            int daysInDatabase = (DateTime.Now - product.submit_date).Days;

            // Разница между последней и первой позицией
            int posDifference = 0;
            if (product.parse_data != null && product.parse_data.Count > 0)
            {
                posDifference = product.parse_data.Last().pos - product.parse_data.First().pos;
            }

            // Добавляем только название продукта и описание в экранированном виде
            captionBuilder.AppendLine($"[{product.parse_data.Last().pos}  {EscapeMarkdown(product.product_name)}]({product.product_url})");
           
            captionBuilder.AppendLine($"Дней в базе {EscapeMarkdown(daysInDatabase.ToString())}");
            captionBuilder.AppendLine($"Разница в позициях {EscapeMarkdown(posDifference.ToString())}");
        }

        return captionBuilder.ToString();
    }


    public static string EscapeMarkdown(string input)
    {
        return input
            .Replace("_", "\\_")
            .Replace("*", "\\*")
            .Replace("[", "\\[")
            .Replace("]", "\\]")
            .Replace("(", "\\(")
            .Replace(")", "\\)")
            .Replace("~", "\\~")
            .Replace("`", "\\`")
            .Replace(">", "\\>")
            .Replace("#", "\\#")
            .Replace("+", "\\+")
            .Replace("-", "\\-")  // Экранируем дефис только в тексте
            .Replace("=", "\\=")
            .Replace("|", "\\|")
            .Replace("{", "\\{")
            .Replace("}", "\\}")
            .Replace("!", "\\!");
    }



    public static async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        // Получаем идентификатор чата и информацию о текущем запросе
        var chatId = callbackQuery.Message.Chat.Id;
        var callbackData = callbackQuery.Data;

        if (userSessions.TryGetValue(chatId, out UserSession session))
        {
            // Извлекаем номер страницы из callbackData
            if (callbackData.StartsWith("page_"))
            {
                int newPageIndex;
                if (int.TryParse(callbackData.Split('_')[1], out newPageIndex))
                {
                    // Обновляем текущий индекс страницы в сессии
                    session.CurrentPageIndex = newPageIndex;

                    // Отправляем новую страницу галереи
                    await SendGalleryAsync(chatId, session.Products, newPageIndex, session.PageSize);

                    // Закрываем callback запрос
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                }
            }
        }
    }



    public class UserSession
    {
        public List<int> MessageIds { get; set; } = new List<int>();
        public int SignatureMessageId { get; set; } = 0; // Добавлено для хранения идентификатора сообщения с подписью
        public int CurrentPageIndex { get; set; } = 0;
        public int TotalPages { get; set; } = 0;
        public int PageSize { get; set; } = 5;
        public List<Product> Products { get; set; }
    }
}