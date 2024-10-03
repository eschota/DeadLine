using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;

internal class SDAdapter
{
    private static Process sdProcess;

    public static async Task StartStableDiffusion()
    {
        await Task.Run(() =>
        {
            try
            {
                if (sdProcess != null && !sdProcess.HasExited)
                {
                    sdProcess.Kill();
                    sdProcess.WaitForExit();
                }

                sdProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/C python launch.py --api --no-half-vae",
                        WorkingDirectory = @"c:\stable-diffusion-webui",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                sdProcess.OutputDataReceived += ProcessOutputHandler;
                sdProcess.ErrorDataReceived += ProcessOutputHandler;
                sdProcess.Exited += (sender, e) => Console.WriteLine("Stable Diffusion process exited.");

                sdProcess.Start();
                sdProcess.BeginOutputReadLine();
                sdProcess.BeginErrorReadLine();

                Console.WriteLine("Stable Diffusion process started.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start Stable Diffusion process: {ex.Message}");
            }
        });
    }
    private static readonly HttpClient httpClient = new HttpClient();
    public static async Task<bool> CheckServiceAvailability(string url)
    {
        try
        {
            var response = await httpClient.GetAsync(url);

            // Возвращаем true, если статус-код 2xx (успешный запрос)
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            // Логируем исключение (опционально)
            Console.WriteLine($"Exception occurred: {ex.Message}");
            return false;
        }
    }
    public static async Task RestartStableDiffusion()
    {
       // if (await CheckServiceAvailability("http://127.0.0.1:7860")) return;
        StopStableDiffusion();
        Console.WriteLine("Restarting Stable Diffusion process.");
        await StartStableDiffusion();
        await Task.Delay(30000);
        Console.WriteLine("Complete Restarting Diffusion process.");
    }

    public static void StopStableDiffusion()
    {
        try
        {
            if (sdProcess != null && !sdProcess.HasExited)
            {
                sdProcess.Kill();
                sdProcess.WaitForExit();
                Console.WriteLine("Stable Diffusion process stopped.");
            }

            var pythonProcesses = Process.GetProcessesByName("python");
            foreach (var process in pythonProcesses)
            {
                try
                {
                    process.Kill();
                    process.WaitForExit();
                    Console.WriteLine($"Process {process.Id} (python.exe) stopped.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to stop process {process.Id} (python.exe): {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to stop Stable Diffusion process: {ex.Message}");
        }
    } 

    private static void ProcessOutputHandler(object sender, DataReceivedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Data))
            return;

        Console.WriteLine(e.Data);

        // Check for error patterns in the output
        if (IsErrorInOutput(e.Data))
        {
            Console.WriteLine("Error detected in Stable Diffusion process output. Restarting process...");
            RestartStableDiffusion();
        }
    }

    private static bool IsErrorInOutput(string output)
    {
        // Define your error patterns here
        string[] errorPatterns = { "AssertionError", "InternalServerError", "Traceback" };

        foreach (var pattern in errorPatterns)
        {
            if (Regex.IsMatch(output, pattern, RegexOptions.IgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
    public static async Task RunWithCheckAndRestart(Func<Task> action)
    {
        if (!await CheckSDStatus())
        {
            Console.WriteLine("Stable Diffusion is not responding. Restarting...");
            RestartStableDiffusion();
            await Task.Delay(TimeSpan.FromSeconds(60)); // Wait for 30 seconds before retrying
        }
        await action();
    }

    private static async Task<bool> CheckSDStatus()
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                HttpResponseMessage response = await client.GetAsync("http://127.0.0.1:7860/sdapi/v1/txt2img");
                return response.IsSuccessStatusCode;
            }
        }
        catch
        {
            return false;
        }
    }
}
