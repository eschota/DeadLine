using System.Diagnostics;
using System.Text;

internal class Ollama
{
    public static async Task<string> AskLLama(string prompt)
    {
        return await Task.Run(() =>
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "ollama",
                Arguments = $"run llama3 \"{prompt}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            try
            {
                using (var process = new Process { StartInfo = processStartInfo })
                {
                    var output = new StringBuilder();
                    var error = new StringBuilder();

                    process.OutputDataReceived += (sender, args) =>
                    {
                        if (args.Data != null)
                            output.AppendLine(args.Data);
                    };

                    process.ErrorDataReceived += (sender, args) =>
                    {
                        if (args.Data != null &&
                            !args.Data.Contains("failed to get console mode for stdout: The handle is invalid.") &&
                            !args.Data.Contains("failed to get console mode for stderr: The handle is invalid."))
                        {
                            error.AppendLine(args.Data);
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();

                    if (process.ExitCode != 0 && error.Length > 0)
                    {
                        return $"Error: {error}";
                    }

                    // Filter out specific unwanted messages from the output
                    var filteredOutput = FilterUnwantedMessages(output.ToString());

                    return filteredOutput;
                }
            }
            catch (Exception ex)
            {
                return $"Exception: {ex.Message}";
            }
        });
    }

    private static string FilterUnwantedMessages(string output)
    {
        var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        var filteredLines = lines.Where(line =>
            !line.Contains("failed to get console mode for stdout: The handle is invalid.") &&
            !line.Contains("failed to get console mode for stderr: The handle is invalid.")
        );

        return string.Join(Environment.NewLine, filteredLines);
    }
}