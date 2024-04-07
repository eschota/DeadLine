using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Reflection.PortableExecutable;
using System.Reflection.Emit;

namespace OpenAIClient
{
    internal static class Logger
    {
        public enum LogLevel { Exceptions = 3, Updates = 2, Silent = 1 }


        private static readonly object logLock = new object();
        private static Stopwatch stopwatch = new Stopwatch();
        public static int LogCounter = 0;
        static string tenProbels = "..........";
        public static void AddLog(string line, LogLevel level = LogLevel.Silent, [CallerMemberName] string caller = "")
        {

            lock (logLock)
            {
                try
                {
                    ClearLog();
                    OpenLog(line, level, caller);
                    CloseLog("",level,caller );
                    if (LogCounter % 500 == 0) // Например, каждый 500-й лог
                    {
                        TrimLogFile();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{tenProbels}\nCant Write To LogFile: {ex}\n{tenProbels}");
                }
                LogCounter++;
            }
           
        } 

        public static void OpenLog(string line, LogLevel level, [CallerMemberName] string caller = "")
        {
             
            lock (logLock)
            {
                try
                {
                    string header = GenerateHeaderFooter($"[OpenLog by {caller}] ");
                    AppendTextToFile(header + "    " + line);
                    if(level>0)    Console.WriteLine(header + " " + line);
                    stopwatch.Restart(); // Запускаем таймер
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during OpenLog: {ex.Message}");
                }
            }
        }

        public static void InsertLog(string line, LogLevel level, [CallerMemberName] string caller = "")
        {
             lock (logLock)
            {
                try
                {
                    string header = GenerateHeaderFooter($"[OpenLog by {caller}] ");
                    string content = $" {line}\n";
                    AppendTextToFile(content);
                    if (level > 0) { Console.WriteLine(header + "    " + line); Console.WriteLine(content); }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during InsertLog: {ex.Message}");
                }
            }
        }

        public static void CloseLog(string line, LogLevel level, [CallerMemberName] string caller = "")
        { 
            lock (logLock)
            {
                try
                {
                    stopwatch.Stop(); // Остановка таймера
                    if (!string.IsNullOrEmpty(line))
                    {
                        string header = GenerateHeaderFooter($"[OpenLog by {caller}] ");
                        string content = $"    {line}";
                        AppendTextToFile(content);
                        if (level > 0) { Console.WriteLine(header + "    " + line); Console.WriteLine(content); }
                    }
                    string footer = GenerateHeaderFooter($"[CloseLog by {caller}] {line}");
                    AppendTextToFile(footer);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during CloseLog: {ex.Message}");
                }
                LogCounter++;
            }
        }

        private static string GenerateHeaderFooter(string title)
        {
            return $"\n[{"Parser: "} {title} {LogCounter:00} Time: {DateTime.Now:HH:mm:ss}]\n";
        }

        private static void ClearLog()
        {
            if (LogCounter == 0 && File.Exists(Paths.LogFile))
            {
                File.Delete(Paths.LogFile);
            }
        }

        private static void AppendTextToFile(string text)
        {
            try
            {
                File.AppendAllText(Paths.LogFile, text);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error appending to log file: {ex.Message}");
            }
        }
        private static void TrimLogFile()
        {
            try
            {
                if (File.Exists(Paths.LogFile))
                {
                    var lines = File.ReadAllLines(Paths.LogFile);
                    if (lines.Length > 500) // Если записей больше, чем нужно сохранить
                    {
                        // Оставляем только последние 500 записей
                        var newLines = lines[^500..];
                        File.WriteAllLines(Paths.LogFile, newLines);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during TrimLogFile: {ex.Message}");
            }
        }
    }
    
}
