namespace PoePickit.Parser
{
    using System;
    using System.IO;

    using Guards;

    public abstract class PricerDataReader
    {
        private readonly string dataRootDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

        protected PricerDataReader(string filePath)
        {
            Guard.ArgumentNotNullOrEmpty(filePath, nameof(filePath));
            if (!Path.IsPathRooted(filePath))
            {
                filePath = Path.Combine(dataRootDir, filePath);
            }
            if (!Path.HasExtension(filePath))
            {
                filePath = Path.ChangeExtension(filePath, "txt");
            }
            FilePath = filePath;
            RawLines = ReadRawLines();
        }

        public string FileName => Path.GetFileNameWithoutExtension(FilePath);

        public string FilePath { get; }

        public string[] RawLines { get; }

        protected string[] ReadRawLines()
        {
            var fileData = File.ReadAllLines(FilePath);
            return fileData;
        }
    }
}