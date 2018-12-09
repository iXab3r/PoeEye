using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Audio.Services
{
    internal class FileSoundLibrarySource : SoundLibrarySourceBase
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(FileSoundLibrarySource));

        private static readonly DirectoryInfo ResourceDirectory =
            new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Notifications"));

        public FileSoundLibrarySource()
        {
            var extensions = GetSupportedExtensions();
            var sources = ResourceDirectory.Exists
                ? ResourceDirectory
                    .EnumerateFiles()
                    .Select(x => new {FilePath = x.FullName, SourceName = Path.GetFileNameWithoutExtension(x.Name)})
                    .Where(x => extensions.Any(ext => string.Equals(ext, Path.GetExtension(x.FilePath), StringComparison.OrdinalIgnoreCase)))
                    .Select(x => x.SourceName)
                    .ToArray()
                : new string[0];
            SourceName = sources;

            Log.Debug($"Source name list(directory: {ResourceDirectory.FullName}):\r\n {sources.DumpToText()}");
        }

        public override IEnumerable<string> SourceName { get; }

        public override bool TryToLoadSourceByName(string name, out byte[] resourceData)
        {
            var filePaths = FormatFileName(name)
                .Select(FormatSourceFileName)
                .ToArray();
            Log.Debug($"Looking up files '{filePaths.DumpToTextRaw()}'...");
            var soundFile = filePaths.FirstOrDefault(File.Exists);
            if (string.IsNullOrEmpty(soundFile))
            {
                Log.Debug($"File was not found '{name}', candidates: {filePaths.DumpToTextRaw()}");
                resourceData = null;
                return false;
            }

            try
            {
                resourceData = LoadAudioStream(soundFile);

                Log.Debug($"Loaded file '{soundFile}' : {resourceData.Length}b");
                return true;
            }
            catch (Exception e)
            {
                Log.Error($"Failed to load source {soundFile}", e);
                resourceData = null;
                return false;
            }
        }

        private string FormatSourceFileName(string fileName)
        {
            return Path.Combine(ResourceDirectory.FullName, fileName);
        }

        private byte[] LoadAudioStream(string fileName)
        {
            using (var mediaStream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return mediaStream.ReadToEnd();
            }
        }
    }
}