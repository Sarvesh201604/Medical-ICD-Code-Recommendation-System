using System;
using System.IO;

public static class DebugLogger
{
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Sonocare",
        "Logs"
    );

    private static readonly string LogFile = Path.Combine(LogDirectory, "debug_voice.log");

    static DebugLogger()
    {
        try
        {
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }
        }
        catch { }
    }

    public static void Log(string message)
    {
        try
        {
            File.AppendAllText(LogFile, $"{DateTime.Now:HH:mm:ss} {message}\n");
        }
        catch { }
    }
}
