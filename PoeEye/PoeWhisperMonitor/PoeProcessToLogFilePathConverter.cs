namespace PoeWhisperMonitor
{
    using System.IO;

    internal sealed class PoeProcessToLogFilePathConverter
    {
        public FileInfo Convert(PoeProcessInfo poeProcess)
        {
            var logsDirectory = poeProcess.Executable.DirectoryName;

            var logFilePath = Path.Combine(logsDirectory, "logs", "Client.txt");

            return new FileInfo(logFilePath);
        }
    }
}