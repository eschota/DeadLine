using System;  
using System.Diagnostics;
using System.Text;
internal static class Audio
{
    public static string ConvertOggToWav(string input)
    {
        ProcessStartInfo start = new ProcessStartInfo();
        start.FileName = Paths.whisper;
        string args = $"\"{input}\" --language=ru";
         
        start.Arguments = args;// -output \"{output}\""; // python script_path image_path text_query
        start.Arguments = start.Arguments.Trim(' ');
        start.UseShellExecute = false;
        start.RedirectStandardOutput = true; // read output
        start.RedirectStandardError = true; // read error
        start.CreateNoWindow = true; // no window
        start.StandardOutputEncoding = Encoding.UTF8;
        start.StandardErrorEncoding = Encoding.UTF8;


        string result = "";

        using (Process process = Process.Start(start))
        {
            using (StreamReader reader = process.StandardOutput)
            {
                result = reader.ReadToEnd(); // read output
            }

            using (StreamReader reader = process.StandardError)
            {
                string error = reader.ReadToEnd(); // read error
                if (!string.IsNullOrEmpty(error))
                {

                }
            }
        }
        if (!String.IsNullOrEmpty(result))
            return result.Substring(result.IndexOf("]") + 1,result.IndexOf("Transcription")- result.IndexOf("]") + 1);
        else return null;
    }
}