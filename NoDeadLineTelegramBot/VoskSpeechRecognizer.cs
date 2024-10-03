using System;
using System.Text;
using NAudio.Wave;
using Vosk;
using System.Text.Json;
using Microsoft.VisualBasic;
using System.Reflection;

 
    //public class VoskSpeechRecognizer : IDisposable
    //{
    //    private readonly WaveInEvent _waveIn;
    //    private readonly VoskRecognizer _recognizer;
    //    private readonly StringBuilder _resultBuilder;
    //    private readonly object _lock = new object();

    //    // Состояние распознавания
    //    private enum RecognizerState
    //    {
    //        Idle,           // Ожидание ключевого слова "БОТ"
    //        Listening       // Слушаем вопрос после "БОТ" до "ДАВАЙ"
    //    }

    //    private RecognizerState _currentState = RecognizerState.Idle;
    //    private StringBuilder _questionBuilder = new StringBuilder();

    //    // События
    //    public event Action<string> OnFinalResult;
    //    public event Action<string> OnPartialResult;
    //    public event Action<string> OnError;

    //    // Функция обратного вызова для обработки вопроса
    //    public Action<string> userAskQuestion { get; set; }

    //    public VoskSpeechRecognizer(string modelPath, string language = "ru-RU")
    //    {
    //        // Инициализация Vosk
    //        Vosk.Vosk.SetLogLevel(0); // Отключаем логи
    //        var model = new Model(modelPath);
    //        _recognizer = new VoskRecognizer(model, 16000.0f);
    //        _recognizer.SetMaxAlternatives(0);
    //        _recognizer.SetWords(false);
    //        _resultBuilder = new StringBuilder();

    //        // Настройка микрофона
    //        _waveIn = new WaveInEvent
    //        {
    //            DeviceNumber = 0, // Используем устройство по умолчанию
    //            WaveFormat = new WaveFormat(16000, 1) // 16 кГц, моно
    //        };
    //        _waveIn.DataAvailable += WaveIn_DataAvailable;
    //        _waveIn.RecordingStopped += WaveIn_RecordingStopped;
    //    }

    //    public void Start()
    //    {
    //        try
    //        {
    //            _waveIn.StartRecording();
    //            Console.WriteLine("Начало записи. Говорите...");
    //        }
    //        catch (Exception ex)
    //        {
    //            OnError?.Invoke($"Ошибка при запуске записи: {ex.Message}");
    //        }
    //    }
    //  public  Task StartSpeechRecognitionAsync()
    //    {
    //        return Task.Run(() =>
    //        {
    //            string modelPath = "model"; // Путь к вашей модели Vosk

    //            using var recognizer = new VoskSpeechRecognizer(modelPath)
    //            {
    //                userAskQuestion = UserAskQuestion // Назначаем обработчик вопроса
    //            };

    //            // Подписка на события (опционально)
    //            recognizer.OnFinalResult += (text) =>
    //            {
    //                // Дополнительная обработка окончательных результатов, если необходимо
    //            };

    //            recognizer.OnPartialResult += (partial) =>
    //            {
    //                // Можно отображать частичные результаты в реальном времени
    //                // Console.WriteLine($"Частичный результат: {partial}");
    //            };

    //            recognizer.OnError += (error) =>
    //            {
    //                Console.WriteLine($"Ошибка: {error}");
    //            };

    //            recognizer.Start();

    //            Console.WriteLine("Приложение запущено. Говорите ключевые слова 'БОТ' и 'ДАВАЙ' для взаимодействия.");
    //            Console.WriteLine("Нажмите любую клавишу для остановки...");

    //            Console.ReadKey();

    //            recognizer.Stop();

    //            // Даем время на корректное завершение
    //            Task.Delay(500).Wait();
    //        });
    //    }
    //    static void UserAskQuestion(string question)
    //    {
    //        Console.WriteLine($"Обработка вопроса: {question}");
    //        // Здесь вы можете добавить вашу логику обработки вопроса
    //        // Например, отправить вопрос в ChatGPT и вывести ответ

    //        // Пример:
    //        // string answer = ChatGPTService.GetAnswer(question);
    //        // Console.WriteLine($"Ответ Бота: {answer}");
    //    }
    //    public void Stop()
    //    {
    //        _waveIn.StopRecording();
    //    }

    //    private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
    //    {
    //        if (_recognizer.AcceptWaveform(e.Buffer, e.BytesRecorded))
    //        {
    //            var result = _recognizer.Result();
    //            ProcessResult(result);
    //        }
    //        else
    //        {
    //            var partialResult = _recognizer.PartialResult();
    //            ProcessPartialResult(partialResult);
    //        }
    //    }

    //    private void WaveIn_RecordingStopped(object sender, StoppedEventArgs e)
    //    {
    //        _recognizer?.Dispose();
    //        _waveIn?.Dispose();
    //        if (e.Exception != null)
    //        {
    //            OnError?.Invoke($"Ошибка записи: {e.Exception.Message}");
    //        }
    //        Console.WriteLine("Запись остановлена.");
    //    }

    //    private void ProcessResult(string jsonResult)
    //    {
    //        try
    //        {
    //            var json = JsonDocument.Parse(jsonResult);
    //            if (json.RootElement.TryGetProperty("text", out var textElement))
    //            {
    //                var text = textElement.GetString()?.Trim().ToUpper();
    //                if (!string.IsNullOrEmpty(text))
    //                {
    //                    HandleRecognizedText(text);
    //                }
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            OnError?.Invoke($"Ошибка разбора результата: {ex.Message}");
    //        }
    //    }

    //    private void ProcessPartialResult(string jsonPartial)
    //    {
    //        try
    //        {
    //            var json = JsonDocument.Parse(jsonPartial);
    //            if (json.RootElement.TryGetProperty("partial", out var partialElement))
    //            {
    //                var partialText = partialElement.GetString()?.Trim().ToUpper();
    //                if (!string.IsNullOrEmpty(partialText))
    //                {
    //                    // Вы можете обрабатывать частичные результаты при необходимости
    //                    // Например, отображать их в реальном времени
    //                    OnPartialResult?.Invoke(partialText);
    //                }
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            OnError?.Invoke($"Ошибка разбора частичного результата: {ex.Message}");
    //        }
    //    }

    //    private void HandleRecognizedText(string text)
    //    {
    //        switch (_currentState)
    //        {
    //            case RecognizerState.Idle:
    //                if (text.Contains("БОТ"))
    //                {
    //                    _currentState = RecognizerState.Listening;
    //                    _questionBuilder.Clear();
    //                    Console.WriteLine("Ключевое слово 'БОТ' обнаружено. Начинаю запись вопроса...");
    //                }
    //                break;

    //            case RecognizerState.Listening:
    //                if (text.Contains("ДАВАЙ"))
    //                {
    //                    // Завершаем запись вопроса
    //                    var question = _questionBuilder.ToString().Trim();
    //                    Console.WriteLine($"Окончание записи вопроса обнаружено. Вопрос: {question}");
    //                    userAskQuestion?.Invoke(question);
    //                    _currentState = RecognizerState.Idle;
    //                }
    //                else
    //                {
    //                    // Добавляем текст к вопросу
    //                    _questionBuilder.AppendLine(text);
    //                    Console.WriteLine($"Распознано: {text}");
    //                }
    //                break;
    //        }
    //    }

    //    public void Dispose()
    //    {
    //        _waveIn?.Dispose();
    //        _recognizer?.Dispose();
    ////    }
    //} 