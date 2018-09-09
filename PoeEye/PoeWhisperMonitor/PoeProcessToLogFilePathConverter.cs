using System.IO;

namespace PoeWhisperMonitor
{
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