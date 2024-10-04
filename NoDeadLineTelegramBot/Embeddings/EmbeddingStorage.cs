using System.IO.Compression;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using static iMessage;
using System.Windows.Forms;


public class EmbeddingStorage
{


// Метод для сохранения массива float и объекта iMessage с адаптивным сохранением в бинарном формате с сжатием
public static void SaveEmbedding(float[] embedding, iMessage message, string fileName)
{ 
    using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
    using (GZipStream gzipStream = new GZipStream(fs, CompressionMode.Compress))
    using (BinaryWriter writer = new BinaryWriter(gzipStream))
    {
        // Сохраняем длину массива
        writer.Write(embedding.Length);
        foreach (float value in embedding)
        {
            writer.Write(value);
        }

        // Адаптивное сохранение объекта iMessage
        WriteiMessageBinary(writer, message);
    }
}

// Метод для записи объекта iMessage в бинарном формате с учетом изменений полей
private static void WriteiMessageBinary(BinaryWriter writer, iMessage message)
{
    var messageType = message.GetType();
    var fields = messageType.GetFields();

    // Записываем количество полей
    writer.Write(fields.Length);

    foreach (var field in fields)
    {
        var fieldValue = field.GetValue(message);

        // Записываем имя поля
        writer.Write(field.Name);

        // Адаптивно сохраняем значение в зависимости от его типа
        switch (fieldValue)
        {
            case int intValue:
                writer.Write(intValue);
                break;
            case long longValue:
                writer.Write(longValue);
                break;
            case float floatValue:
                writer.Write(floatValue);
                break;
            case double doubleValue:
                writer.Write(doubleValue);
                break;
            case bool boolValue:
                writer.Write(boolValue);
                break;
            case string stringValue:
                writer.Write(stringValue ?? string.Empty);
                break;
            case float[] floatArrayValue:
                writer.Write(floatArrayValue.Length); // Сохраняем длину массива
                foreach (float value in floatArrayValue)
                {
                    writer.Write(value); // Сохраняем элементы массива
                }
                break;
            case int[] intArrayValue:
                writer.Write(intArrayValue.Length); // Сохраняем длину массива
                foreach (int value in intArrayValue)
                {
                    writer.Write(value); // Сохраняем элементы массива
                }
                break;
                case DateTime dateTimeValue:
                    writer.Write(dateTimeValue.ToBinary());
                    break;

                default:
                throw new InvalidOperationException($"Неизвестный тип поля: {field.FieldType.Name}");
        }
    }
}

// Метод для загрузки сжатого бинарного файла с массивом float и объектом iMessage
public static iMessage LoadEmbedding(string fileName, iMessage messageTemplate)
{
    using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
    using (GZipStream gzipStream = new GZipStream(fs, CompressionMode.Decompress))
    using (BinaryReader reader = new BinaryReader(gzipStream))
    {
        int length = reader.ReadInt32();
        float[] embedding = new float[length];

        for (int i = 0; i < length; i++)
        {
            embedding[i] = reader.ReadSingle();
        }

        // Адаптивное восстановление объекта iMessage
        iMessage message = ReadiMessageBinary(reader, messageTemplate);
            message.embeddings = embedding;
            return message;
    }
}

// Метод для чтения объекта iMessage из бинарного формата
private static iMessage ReadiMessageBinary(BinaryReader reader, iMessage messageTemplate)
{
    var messageType = messageTemplate.GetType();
    var fields = messageType.GetFields();

    // Читаем количество полей
    int fieldCount = reader.ReadInt32();

    for (int i = 0; i < fieldCount; i++)
    {
        // Читаем имя поля
        string fieldName = reader.ReadString();

        // Находим поле по имени
        var field = messageType.GetField(fieldName);
        if (field != null)
        {
            // Адаптивно восстанавливаем значение в зависимости от типа поля
            switch (field.FieldType.Name)
            {
                case nameof(Int32):
                    field.SetValue(messageTemplate, reader.ReadInt32());
                    break;
                case nameof(Int64):
                    field.SetValue(messageTemplate, reader.ReadInt64());
                    break;
                case nameof(Single):
                    field.SetValue(messageTemplate, reader.ReadSingle());
                    break;
                case nameof(Double):
                    field.SetValue(messageTemplate, reader.ReadDouble());
                    break;
                case nameof(Boolean):
                    field.SetValue(messageTemplate, reader.ReadBoolean());
                    break;
                case nameof(String):
                    field.SetValue(messageTemplate, reader.ReadString());
                    break;
                case nameof(Single) + "[]":
                    int floatArrayLength = reader.ReadInt32();
                    float[] floatArray = new float[floatArrayLength];
                    for (int j = 0; j < floatArrayLength; j++)
                    {
                        floatArray[j] = reader.ReadSingle();
                    }
                    field.SetValue(messageTemplate, floatArray);
                    break;
                case nameof(Int32) + "[]":
                    int intArrayLength = reader.ReadInt32();
                    int[] intArray = new int[intArrayLength];
                    for (int j = 0; j < intArrayLength; j++)
                    {
                        intArray[j] = reader.ReadInt32();
                    }
                    field.SetValue(messageTemplate, intArray);
                    break;
                    case nameof(DateTime):
                        field.SetValue(messageTemplate, DateTime.FromBinary(reader.ReadInt64()));
                        break;

                    default:
                    throw new InvalidOperationException($"Неизвестный тип поля: {field.FieldType.Name}");
            }
        }
    }

    return messageTemplate;
}


}
