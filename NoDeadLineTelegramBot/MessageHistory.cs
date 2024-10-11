using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static CGTrendsAPI;
using Telegram.Bot;
using Telegram.Bot.Types;
using static Chat;
using System.Diagnostics.Contracts;



public class MessageHistory
{
    public Dictionary<string, List<iMessage>> _messagesByChatId;
    private static MessageHistory _instance;

    // Приватный конструктор для синглтона
    private MessageHistory()
    {
        _messagesByChatId = new Dictionary<string, List<iMessage>>();
    }

    // Метод для получения экземпляра класса (синглтон)
    public static MessageHistory Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new MessageHistory();
            }
            return _instance;
        }
    }

    // Метод для инициализации и загрузки всех сообщений
    public void LoadAllMessages(string chatHistoryPath)
    {
        var directoryInfo = new DirectoryInfo(chatHistoryPath);
        var files = directoryInfo.GetFiles("*.bin");

        foreach (var file in files)
        {
            try
            {
                var iMessage = LoadMessage(file.FullName);
                if (iMessage != null)
                {
                    if (!_messagesByChatId.ContainsKey(iMessage.chat_id.ToString()))
                    {
                        _messagesByChatId[iMessage.chat_id.ToString()] = new List<iMessage>();
                    }

                    _messagesByChatId[iMessage.chat_id.ToString()].Add(iMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке сообщения из файла {file.Name}: {ex.Message}");
            }
        }
    }

    public List<(iMessage message, double avgDistance, List<(iMessage relatedMessage, double distance)> relatedMessages)> FindClosestMessageBranch(float[] embeddings, int count = 10, double threshold = 0.5)
    {
        List<(iMessage message, double distance)> closestMessages = new List<(iMessage message, double distance)>();

        if (embeddings.Length == 0)
            return new List<(iMessage message, double avgDistance, List<(iMessage relatedMessage, double distance)> relatedMessages)>();

        // Находим ближайшие сообщения по embeddings
        foreach (var chatMessages in _messagesByChatId.Values)
        {
            foreach (var message in chatMessages)
            {
                if (message.embeddings.Length == 0) continue;

                double distance = Vectors.CosineDistance(embeddings, message.embeddings);

                // Добавляем сообщение с его дистанцией в список, если расстояние меньше порога
                if (distance < threshold)
                {
                    closestMessages.Add((message, distance));
                }
            }
        }

        // Сортируем по возрастанию distance
        closestMessages = closestMessages.OrderBy(x => x.distance).Take(count).ToList();

        // Выходной список: сообщение, средняя дистанция, связанные сообщения
        List<(iMessage message, double avgDistance, List<(iMessage relatedMessage, double distance)> relatedMessages)> result = new List<(iMessage message, double avgDistance, List<(iMessage relatedMessage, double distance)> relatedMessages)>();

        foreach (var (mainMessage, _) in closestMessages)
        {
            // Ищем ближайшие по времени сообщения к mainMessage
            var nearbyMessages = FindNearbyMessagesByTime(mainMessage);

            // Для хранения связанных сообщений и их расстояний
            List<(iMessage relatedMessage, double distance)> relatedMessages = new List<(iMessage relatedMessage, double distance)>();

            // Находим расстояние для каждого ближайшего по времени сообщения
            double totalDistance = 0;
            int nearbyCount = 0;

            foreach (var nearbyMessage in nearbyMessages)
            {
                // Расстояние между эмбеддингами ближайших по времени сообщений
                if (embeddings.Length == 0 || nearbyMessage.embeddings.Length == 0) continue;
                double distance = Vectors.CosineDistance(embeddings, nearbyMessage.embeddings);

                // Если расстояние меньше 0.5, добавляем сообщение в связанные
                if (distance < 0.5)
                {
                    relatedMessages.Add((nearbyMessage, distance));
                }

                totalDistance += distance;
                nearbyCount++;
            }

            // Если есть ближайшие сообщения, считаем среднюю дистанцию
            double avgDistance = nearbyCount > 0 ? totalDistance / nearbyCount : double.MaxValue;

            // Добавляем результат: основное сообщение, средняя дистанция, связанные сообщения
            result.Add((mainMessage, avgDistance, relatedMessages));
        }

        // Сортируем по возрастанию средней дистанции и возвращаем
        return result.OrderBy(x => x.avgDistance).ToList();
    }

    public List<(iMessage message, double avgDistance, List<(iMessage relatedMessage, double distance)> relatedMessages)> FindClosestMessageBranchInDepth(float[] embeddings, double threshold = 0.5, int timeWindowMinutes = 20)
    {
        List<(iMessage message, double distance)> closestMessages = new List<(iMessage message, double distance)>();

        if (embeddings.Length == 0) return new List<(iMessage message, double avgDistance, List<(iMessage relatedMessage, double distance)> relatedMessages)>();

        // Находим ближайшие сообщения по embeddings с помощью поиска по всему словарю
        foreach (var chatMessages in _messagesByChatId.Values)
        {
            foreach (var message in chatMessages)
            {
                if (message.embeddings.Length == 0) continue;

                double distance = Vectors.CosineDistance(embeddings, message.embeddings);

                // Добавляем сообщение с его дистанцией в список, если расстояние меньше порога
                if (distance < threshold)
                {
                    closestMessages.Add((message, distance));
                }
            }
        }

        // Сортируем по возрастанию distance
        closestMessages = closestMessages.OrderBy(x => x.distance).ToList();

        // Результирующий список ветки без повторов
        HashSet<iMessage> visitedMessages = new HashSet<iMessage>();
        List<(iMessage message, double avgDistance, List<(iMessage relatedMessage, double distance)> relatedMessages)> fullBranch = new List<(iMessage message, double avgDistance, List<(iMessage relatedMessage, double distance)> relatedMessages)>();

        foreach (var (mainMessage, _) in closestMessages)
        {
            // Ищем связанные сообщения в глубину
            List<(iMessage message, double distance)> branch = new List<(iMessage message, double distance)>();
            BuildBranchRecursive(mainMessage, embeddings, branch, visitedMessages, threshold, timeWindowMinutes);

            // Вычисляем среднюю дистанцию для текущей ветки
            double avgDistance = branch.Count > 0 ? branch.Average(x => x.distance) : double.MaxValue;

            // Добавляем уникальные сообщения и их связанные сообщения в результирующий список
            List<(iMessage relatedMessage, double distance)> relatedMessages = new List<(iMessage relatedMessage, double distance)>();

            foreach (var (message, distance) in branch)
            {
                if (!fullBranch.Any(x => x.message.message_id == message.message_id))
                {
                    fullBranch.Add((message, avgDistance, relatedMessages));
                }
                relatedMessages.Add((message, distance)); // Добавляем связанные сообщения
            }
        }

        // Возвращаем полную ветку
        return fullBranch;
    }

    // Рекурсивный метод для поиска сообщений по смыслу в глубину
    private void BuildBranchRecursive(iMessage currentMessage, float[] embeddings, List<(iMessage message, double distance)> branch, HashSet<iMessage> visitedMessages, double threshold, int timeWindowMinutes)
    {
        // Добавляем сообщение в список посещенных, чтобы не обрабатывать его повторно
        if (!visitedMessages.Add(currentMessage)) return;

        // Находим ближайшие по времени сообщения к текущему сообщению
        var nearbyMessages = FindNearbyMessagesByTime(currentMessage, timeWindowMinutes);

        foreach (var nearbyMessage in nearbyMessages)
        {
            if (nearbyMessage.embeddings.Length == 0) continue;

            // Вычисляем расстояние между эмбеддингами
            double distance = Vectors.CosineDistance(embeddings, nearbyMessage.embeddings);

            // Если расстояние меньше порога, добавляем сообщение в ветку
            if (distance < threshold)
            {
                // Проверяем, добавлено ли сообщение уже в ветку
                if (!branch.Any(x => x.message.message_id == nearbyMessage.message_id))
                {
                    branch.Add((nearbyMessage, distance));

                    // Рекурсивно ищем связанные сообщения дальше в глубину
                    BuildBranchRecursive(nearbyMessage, embeddings, branch, visitedMessages, threshold, timeWindowMinutes);
                }
            }
        }
    }

    // Метод для поиска ближайших по времени сообщений из того же chat_id
    private List<iMessage> FindNearbyMessagesByTime(iMessage mainMessage, int timeWindowMinutes = 10)
    {
        List<iMessage> nearbyMessages = new List<iMessage>();

        // Ищем только в сообщениях того же chat_id
        if (_messagesByChatId.TryGetValue(mainMessage.chat_id.ToString(), out var chatMessages))
        {
            foreach (var message in chatMessages)
            {
                // Пропускаем само основное сообщение
                if (message.message_id != mainMessage.message_id)
                {
                    // Проверяем, что сообщение в пределах временного окна (например, 10 минут до или после)
                    TimeSpan timeDiff = message.date_time - mainMessage.date_time;
                    if (Math.Abs(timeDiff.TotalMinutes) <= timeWindowMinutes)
                    {
                        nearbyMessages.Add(message);
                    }
                }
            }

            // Сортируем по времени отправления (ближайшие по времени идут первыми)
            return nearbyMessages.OrderBy(x => Math.Abs((x.date_time - mainMessage.date_time).TotalMinutes)).ToList();
        }

        // Если сообщений из этого чата нет, возвращаем пустой список
        return nearbyMessages;
    }





    // Метод для поиска ближайших по времени сообщений из того же chat_id
    // Метод для поиска ближайших по времени сообщений из того же chat_id
    public List<(iMessage message, double distance)> FindFullMessageBranch(iMessage startMessage, float[] embeddings, double threshold = 0.5, int timeWindowMinutes = 10)
    {
        List<(iMessage message, double distance)> branch = new List<(iMessage message, double distance)>();

        // Запускаем рекурсивный поиск ветки сообщений
        BuildBranchRecursive(startMessage, embeddings, branch, threshold, timeWindowMinutes);

        return branch;
    }

    // Рекурсивный метод для поиска сообщений по смыслу в глубину
    private void BuildBranchRecursive(iMessage currentMessage, float[] embeddings, List<(iMessage message, double distance)> branch, double threshold, int timeWindowMinutes)
    {
        // Находим ближайшие по времени сообщения к текущему сообщению
        var nearbyMessages = FindNearbyMessagesByTime(currentMessage, timeWindowMinutes);

        foreach (var nearbyMessage in nearbyMessages)
        {
            if (nearbyMessage.embeddings.Length == 0) continue;

            // Вычисляем расстояние между эмбеддингами
            double distance = Vectors.CosineDistance(embeddings, nearbyMessage.embeddings);

            // Если расстояние меньше порога, добавляем сообщение в ветку
            if (distance < threshold)
            {
                // Проверяем, добавлено ли сообщение уже в ветку, чтобы избежать циклов
                if (!branch.Any(x => x.message.message_id == nearbyMessage.message_id))
                {
                    branch.Add((nearbyMessage, distance));

                    // Рекурсивно ищем связанные сообщения дальше в глубину
                    BuildBranchRecursive(nearbyMessage, nearbyMessage.embeddings, branch, threshold, timeWindowMinutes);
                }
            }
        }
    }

    // Метод для поиска ближайших по времени сообщений из того же chat_id
    
    // Метод для поиска ближайших по времени сообщений из того же chat_id


    // Метод для поиска сообщения по вектору эмбеддингов
    public (iMessage? message, double distance) FindClosestMessage(float[] embeddings)
    {
        iMessage? closestMessage = null;
        double minDistance = double.MaxValue;

        if (embeddings.Length == 0) return (null, double.MaxValue);

        foreach (var chatMessages in _messagesByChatId.Values)
        {
            foreach (var message in chatMessages)
            {
                if (message.embeddings.Length == 0) continue;
                double distance = Vectors.CosineDistance(embeddings, message.embeddings);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestMessage = message;
                }
            }
        }

        return (closestMessage, minDistance);
    }



    public List<(iMessage message, double distance)> FindClosestMessages(float[] embeddings, int count = 10)
    {
        List<(iMessage message, double distance)> closestMessages = new List<(iMessage message, double distance)>();

        if (embeddings.Length == 0) return closestMessages;

        //foreach (var chatMessages in _messagesByChatId.Values)
        //{
        //    foreach (var message in chatMessages)
        //    {
        //        if (message.embeddings.Length == 0) continue;

        //        double distance = Vectors.CosineDistance(embeddings, message.embeddings);

        //        // Добавляем сообщение с его дистанцией в список
        //        closestMessages.Add((message, distance));
        //    }
        //}

        // Сортируем список по возрастанию дистанции
        //closestMessages = closestMessages.OrderBy(x => x.distance).ToList();

        // Возвращаем только последние 10 (или меньше, если сообщений меньше 10)
        return closestMessages.Take(count).ToList();
    }


    // Вспомогательный метод для загрузки iMessage из бинарного файла
    private iMessage LoadMessage(string fileName)
    {

            return EmbeddingStorage.LoadEmbedding(fileName, new iMessage());

    }

    public static async Task<bool> FindMessage(Message message)
    {
        try
        {
            string itext = message.Text ?? message.Caption ?? string.Empty;
            if (string.IsNullOrEmpty(itext)) return false;

            itext = itext.ToLower();
            if (itext == "") return false;
            if (!itext.StartsWith("/найди ") && !itext.StartsWith("найди ")) return false;
            itext = itext.StartsWith("/найди ") ? itext.Substring(7) : itext.StartsWith("найди ") ? itext.Substring(6) : itext;
             
            List<ThreadMessage> answer = await Chat.GetFromApi(itext, message.Chat.Id, -1);
            string joinedMessages = string.Join("\n", answer.Select(a => a.Text));
            if (joinedMessages.Length > 0)
            {
               // string a = await OpenAIClient.AskOpenAI($"ответь на вопрос {itext} используя следующий контекст истории сообщений: {joinedMessages}");
                await Chat.Bot.SendTextMessageAsync(message.Chat.Id, $"Ищем {itext}. Вот ОНО! ЕБАТЬ! []\n .\n{joinedMessages}");
            }
            //float [] vector =  await OpenAIClient.AskOpenAI2Embedding(itext);
            //List<(iMessage msg, double d, List<(iMessage relatedMessage, double distance)> relatedMessages)> results = MessageHistory.Instance.FindClosestMessageBranchInDepth(vector);
            //if(results.Count > 0)
            //{
            //    var m = results[0];
            //    if (m.msg.group_id!= message.Chat.Id)
            //    {
            //        await Chat.Bot.SendTextMessageAsync(-4152887032, $"Ищем {itext}. Подсмотрели у других, нашли кое-что похожее. вот текст: " + m.msg.text);
            //        await Chat.Bot.SendTextMessageAsync(message.Chat.Id, $"Ищем {itext}. и нихуя не нашли.");
            //        return true;
            //    }
            //    try
            //    {
            //        var thread = string.Join("\n",
            //             results
            //             .Where(d => d.d < 0.21 && d.relatedMessages != null) // Проверка на null
            //             .SelectMany(m => m.relatedMessages
            //                 .Where(r => r.relatedMessage != null && r.relatedMessage.text != null) // Проверка на null внутри связанных сообщений
            //                 .Select(r => r.relatedMessage.text))
            //             .Distinct());

            //        if (thread.Length == 0) throw new Exception("no thread found");
            //        var construct = await OpenAIClient.AskOpenAI($"Суммируй этот тред, отвечая на вопрос{itext}. 200-600 (в зависимсти от числа информации) символов. Thread:\n{thread}");
            //       await Chat.Bot.SendTextMessageAsync(message.Chat.Id, $"Ищем {itext}. Вот ОНО! ЕБАТЬ! [{m.d}]\n .\n{construct}", replyToMessageId: m.msg.message_id);
            //    }
            //    catch (Exception ex)
            //    {
            //        await Chat.Bot.SendTextMessageAsync(-4152887032, $"Exception: Ищем {itext}.подсмотрели у дркгих, нашли кое-что похожее. вот текст: " + m.msg.text);
            //        await Chat.Bot.SendTextMessageAsync(message.Chat.Id, $"Exception Ищем {itext}. и нихуя не нашли.");
            //        Logger.AddLog($"????????????????: {ex.Message}");
            //    }
            //}






        }
        catch (Exception ex)
        {
            Logger.AddLog($"Ошибка при обработке запроса: {ex.Message}");
        }
        return true;
    }
}
