using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using DynamicData;
using NAudio.Wave;
using PoeShared.Modularity;
using PoeShared.Scaffolding; 
using PoeShared.Logging;

namespace PoeShared.Audio.Services;

internal class FileSoundLibrarySource : SoundLibrarySourceBase, IFileSoundLibrarySource
{
    private static readonly IFluentLog Log = typeof(FileSoundLibrarySource).PrepareLogger();

    private readonly DirectoryInfo[] knownDirectories;

    private readonly SourceCache<FileSource, string> sources = new(x => x.SourceName.ToLowerInvariant());

    public FileSoundLibrarySource(IAppArguments appArguments)
    {
        knownDirectories = new[]
            {
                Path.Combine(appArguments.SharedAppDataDirectory, "Resources", "Notifications"),
                Path.Combine(appArguments.AppDomainDirectory, "Resources", "Notifications"),
            }
            .Distinct()
            .Select(x => new DirectoryInfo(x)).ToArray();

        sources
            .Connect()
            .Transform(x => x.SourceName)
            .Bind(out var sourceNames)
            .Subscribe()
            .AddTo(Anchors);
        SourceName = sourceNames;
            
        Reload();
    }

    public override ReadOnlyObservableCollection<string> SourceName { get; }

    public override bool TryToLoadSourceByName(string name, out byte[] resourceData)
    {
        var source = sources.Lookup(name.ToLowerInvariant());
        if (!source.HasValue)
        {
            Log.Debug(() => $"Source was not found '{name}', loaded files: {sources.Items.Select(x => new {x.SourceName, x.File.FullName}).DumpToString()}");
            resourceData = null;
            return false;
        }

        try
        {
            resourceData = LoadFileData(source.Value.File);
            Log.Debug($"Loaded file '{source.Value}': {resourceData.Length}b");
            return true;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load source {source.Value}", e);
            resourceData = null;
            return false;
        }
    }

    public string AddFromWaveData(string notification, byte[] waveData)
    {
        Guard.ArgumentNotNull(notification, nameof(notification));
        Guard.ArgumentNotNull(waveData, nameof(waveData));

        var directory = GetKnownDirectory();
        var filePath = Path.Combine(directory.FullName, $"{Path.GetFileNameWithoutExtension(notification)}.wav");
        File.WriteAllBytes(filePath, waveData);
        Reload();
        return notification;
    }

    public string AddFromFile(FileInfo soundFile)
    {
        Guard.ArgumentNotNull(soundFile, nameof(soundFile));
        if (!soundFile.Exists)
        {
            throw new FileNotFoundException("File not found", soundFile.FullName);
        }
        Log.Debug(() => $"Trying to add source {soundFile} ({soundFile.Length}b)");

        var directory = GetKnownDirectory();
        var filePath = Path.Combine(directory.FullName, $"{Path.GetFileNameWithoutExtension(soundFile.Name)}.wav");
        using (var reader = new MediaFoundationReader(soundFile.FullName))
        using (WaveStream pcmStream = WaveFormatConversionStream.CreatePcmStream(reader))
        {
            WaveFileWriter.CreateWaveFile(filePath, pcmStream);
        }
        Reload();
        return Path.GetFileNameWithoutExtension(soundFile.Name);
    }

    private DirectoryInfo GetKnownDirectory()
    {
        var directory = knownDirectories.First();
        if (!directory.Exists)
        {
            Log.Debug(() => $"Directory {directory} does not exist, creating it");
            directory.Create();
            directory.Refresh();
        }

        return directory;
    }

    private void Reload()
    {
        Log.Debug(() => $"Updating sound sources, directories:\r\n {knownDirectories.Select(x => new { x.FullName, x.Exists }).DumpToString()}");

        var extensions = GetSupportedExtensions();

        var fileSources =
            (from sourceDirectory in knownDirectories
                where sourceDirectory.Exists
                let files = sourceDirectory.EnumerateFiles()
                from sourceFile in files
                where extensions.Any(ext => string.Equals(ext, sourceFile.Extension, StringComparison.OrdinalIgnoreCase))
                select sourceFile).ToArray();

        var sourcesToRemove = sources.Items.Select(x => x.File).Where(x => !fileSources.Contains(x)).ToArray();
        foreach (var source in sourcesToRemove)
        {
            sources.RemoveKey(new FileSource(source).SourceName);
        }
            
        foreach (var source in fileSources)
        {
            sources.AddOrUpdate(new FileSource(source));
        }
            
        Log.Debug(() => $"Source name list(count: {sources.Count}):\r\n {sources.Items.Select(x => new {x.SourceName, x.File.FullName}).DumpToString()}");
    }

    private static byte[] LoadFileData(FileInfo file)
    {
        using (var mediaStream = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            return mediaStream.ReadToEnd();
        }
    }

    private readonly struct FileSource
    {
        public FileInfo File { get; }
            
        public string SourceName { get; }
            
        public FileSource(FileInfo file) : this()
        {
            File = file;
            SourceName = Path.GetFileNameWithoutExtension(file.Name);
        }

        public override string ToString()
        {
            return $"{File} (exists: {File.Exists})";
        }
    }
}