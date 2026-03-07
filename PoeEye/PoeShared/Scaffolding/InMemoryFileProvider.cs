using DynamicData;
using DynamicData.Kernel;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using PoeShared.IO;

namespace PoeShared.Scaffolding;

/// <summary>
/// Provides an in-memory file provider for serving files and directory contents dynamically.
/// This class implements <see cref="IFileProvider"/> to provide virtual files, 
/// allowing for dynamic creation, updates, and retrieval of file and directory structures.
/// 
/// <para>Key Features:</para>
/// <list type="bullet">
/// <item>Stores files in memory with an <see cref="ISourceCache{TObject, TKey}"/> for efficient access.</item>
/// <item>Handles URL-decoded paths to accommodate browser-originated file requests.</item>
/// <item>Supports retrieving individual files via <see cref="GetFileInfo"/>.</item>
/// <item>Supports listing directory contents via <see cref="GetDirectoryContents"/>.</item>
/// <item>Automatically updates the count of files available.</item>
/// </list>
/// </summary>
public sealed class InMemoryFileProvider : DisposableReactiveObjectWithLogger, IInMemoryFileProvider
{
    private readonly SourceCache<IFileInfo, OSPath> sourceCache = new(x => new OSPath(FileProviderPathUtils.ToStorageKey(FileProviderPathUtils.GetSubpath(x))));

    private volatile FileSystemSnapshot snapshot = FileSystemSnapshot.Create(revision: 0);
    private int modificationRevision;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryFileProvider"/> class.
    /// </summary>
    public InMemoryFileProvider()
    {
        sourceCache.CountChanged.Subscribe(x => Count = x).AddTo(Anchors);
        sourceCache.Connect().Subscribe(_ => Interlocked.Increment(ref modificationRevision)).AddTo(Anchors);
    }

    /// <inheritdoc />
    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        if (!FileProviderPathUtils.TryNormalizeSubpath(subpath, out var normalizedSubpath))
        {
            return NotFoundDirectoryContents.Singleton;
        }

        var currentSnapshot = GetSnapshot();
        return currentSnapshot.DirectoriesByPath.TryGetValue(normalizedSubpath, out var directoryContents)
            ? directoryContents
            : NotFoundDirectoryContents.Singleton;
    }

    /// <inheritdoc />
    public IFileInfo GetFileInfo(string subpath)
    {
        if (!FileProviderPathUtils.TryNormalizeSubpath(subpath, out var normalizedSubpath))
        {
            return new PathAwareNotFoundFileInfo(subpath ?? string.Empty);
        }

        if (string.IsNullOrEmpty(normalizedSubpath))
        {
            return new PathAwareNotFoundFileInfo(normalizedSubpath);
        }

        var currentSnapshot = GetSnapshot();
        return currentSnapshot.FilesByPath.TryGetValue(normalizedSubpath, out var fileInfo)
            ? fileInfo
            : new PathAwareNotFoundFileInfo(normalizedSubpath);
    }

    /// <inheritdoc />
    public IChangeToken Watch(string filter)
    {
        return NullChangeToken.Singleton;
    }

    /// <summary>
    /// Gets the number of files currently stored in memory.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// Provides a reactive source of files stored in memory.
    /// </summary>
    public ISourceCache<IFileInfo, OSPath> FilesByName => sourceCache;

    /// <inheritdoc />
    public IObservable<IChangeSet<IFileInfo, OSPath>> Connect(Func<IFileInfo, bool> predicate = null, bool suppressEmptyChangeSets = true)
    {
        return sourceCache.Connect(predicate, suppressEmptyChangeSets);
    }

    /// <inheritdoc />
    public IObservable<IChangeSet<IFileInfo, OSPath>> Preview(Func<IFileInfo, bool> predicate = null)
    {
        return sourceCache.Preview(predicate);
    }

    /// <inheritdoc />
    public IObservable<Change<IFileInfo, OSPath>> Watch(OSPath key)
    {
        return sourceCache.Watch(ToStorageKey(key));
    }

    /// <inheritdoc />
    public IObservable<int> CountChanged => sourceCache.CountChanged;

    /// <inheritdoc />
    public Optional<IFileInfo> Lookup(OSPath key)
    {
        return sourceCache.Lookup(ToStorageKey(key));
    }

    /// <inheritdoc />
    public IEnumerable<IFileInfo> Items => sourceCache.Items;

    /// <inheritdoc />
    public IEnumerable<OSPath> Keys => sourceCache.Keys;

    /// <inheritdoc />
    public IEnumerable<KeyValuePair<OSPath, IFileInfo>> KeyValues => sourceCache.KeyValues;

    /// <inheritdoc />
    public void Edit(Action<ISourceUpdater<IFileInfo, OSPath>> updateAction)
    {
        sourceCache.Edit(updateAction);
    }

    /// <inheritdoc />
    public Func<IFileInfo, OSPath> KeySelector => sourceCache.KeySelector;

    private FileSystemSnapshot GetSnapshot()
    {
        var currentRevision = Volatile.Read(ref modificationRevision);
        var currentSnapshot = snapshot;
        if (currentSnapshot.Revision == currentRevision)
        {
            return currentSnapshot;
        }

        currentSnapshot = BuildSnapshot(currentRevision);
        snapshot = currentSnapshot;
        return currentSnapshot;
    }

    private FileSystemSnapshot BuildSnapshot(int revision)
    {
        var directoryBuilders = new Dictionary<string, DirectoryBuilder>(FileProviderPathUtils.PathComparer)
        {
            [string.Empty] = new DirectoryBuilder(string.Empty),
        };
        var filesByPath = new Dictionary<string, IFileInfo>(FileProviderPathUtils.PathComparer);

        foreach (var fileInfo in sourceCache.Items.Where(x => x != null))
        {
            var rawSubpath = FileProviderPathUtils.GetSubpath(fileInfo);
            if (!FileProviderPathUtils.TryNormalizeSubpath(rawSubpath, out var subpath) || string.IsNullOrEmpty(subpath))
            {
                continue;
            }

            EnsureDirectory(directoryBuilders, FileProviderPathUtils.GetParentSubpath(subpath));
            UpdateAncestorLastModified(directoryBuilders, subpath, GetLastModified(fileInfo));

            if (IsDirectory(fileInfo))
            {
                var directoryBuilder = EnsureDirectory(directoryBuilders, subpath);
                directoryBuilder.Source = fileInfo;
                directoryBuilder.LastModified = GetLastModified(fileInfo);
                continue;
            }

            var fileEntry = new PathAwareFileInfo(subpath, fileInfo);
            filesByPath[subpath] = fileEntry;
            directoryBuilders[FileProviderPathUtils.GetParentSubpath(subpath)].EntriesByName[fileEntry.Name] = fileEntry;
        }

        var directoriesByPath = directoryBuilders.Keys
            .ToDictionary(
                x => x,
                _ => new InMemoryDirectoryContents(),
                FileProviderPathUtils.PathComparer);
        var directoryEntriesByPath = directoryBuilders.Keys
            .Where(x => !string.IsNullOrEmpty(x))
            .ToDictionary(
                x => x,
                x =>
                {
                    var builder = directoryBuilders[x];
                    return (IFileInfo) new InMemoryDirectoryEntryInfo(builder.Subpath, builder.Source, builder.LastModified);
                },
                FileProviderPathUtils.PathComparer);

        foreach (var directoryEntry in directoryEntriesByPath)
        {
            var parentSubpath = FileProviderPathUtils.GetParentSubpath(directoryEntry.Key);
            var parentBuilder = directoryBuilders[parentSubpath];
            parentBuilder.EntriesByName.TryAdd(directoryEntry.Value.Name, directoryEntry.Value);
        }

        foreach (var directoryBuilder in directoryBuilders.Values)
        {
            directoriesByPath[directoryBuilder.Subpath].SetEntries(directoryBuilder.EntriesByName.Values.ToArray());
        }

        return new FileSystemSnapshot(revision, filesByPath, directoriesByPath);
    }

    private static DirectoryBuilder EnsureDirectory(IDictionary<string, DirectoryBuilder> directoryBuilders, string subpath)
    {
        if (directoryBuilders.TryGetValue(subpath, out var directoryBuilder))
        {
            return directoryBuilder;
        }

        var missingDirectories = new Stack<string>();
        var currentSubpath = subpath;
        while (!directoryBuilders.ContainsKey(currentSubpath))
        {
            missingDirectories.Push(currentSubpath);
            currentSubpath = FileProviderPathUtils.GetParentSubpath(currentSubpath);
        }

        while (missingDirectories.Count > 0)
        {
            var currentDirectorySubpath = missingDirectories.Pop();
            directoryBuilder = new DirectoryBuilder(currentDirectorySubpath);
            directoryBuilders[currentDirectorySubpath] = directoryBuilder;
        }

        return directoryBuilders[subpath];
    }

    private static void UpdateAncestorLastModified(IDictionary<string, DirectoryBuilder> directoryBuilders, string subpath, DateTimeOffset lastModified)
    {
        for (var currentSubpath = FileProviderPathUtils.GetParentSubpath(subpath);; currentSubpath = FileProviderPathUtils.GetParentSubpath(currentSubpath))
        {
            var directoryBuilder = EnsureDirectory(directoryBuilders, currentSubpath);
            if (lastModified > directoryBuilder.LastModified)
            {
                directoryBuilder.LastModified = lastModified;
            }

            if (string.IsNullOrEmpty(currentSubpath))
            {
                return;
            }
        }
    }

    private static bool IsDirectory(IFileInfo fileInfo)
    {
        try
        {
            return fileInfo.IsDirectory;
        }
        catch
        {
            return false;
        }
    }

    private static DateTimeOffset GetLastModified(IFileInfo fileInfo)
    {
        try
        {
            return fileInfo.LastModified;
        }
        catch
        {
            return DateTimeOffset.MinValue;
        }
    }

    private static OSPath ToStorageKey(OSPath key)
    {
        return new OSPath(FileProviderPathUtils.ToStorageKey(key?.FullName));
    }

    private sealed class DirectoryBuilder
    {
        public DirectoryBuilder(string subpath)
        {
            Subpath = subpath;
            EntriesByName = new Dictionary<string, IFileInfo>(FileProviderPathUtils.PathComparer);
        }

        public string Subpath { get; }

        public Dictionary<string, IFileInfo> EntriesByName { get; }

        public IFileInfo Source { get; set; }

        public DateTimeOffset LastModified { get; set; }
    }

    private sealed class FileSystemSnapshot
    {
        public static FileSystemSnapshot Create(int revision)
        {
            var rootDirectory = new InMemoryDirectoryContents();
            rootDirectory.SetEntries(Array.Empty<IFileInfo>());
            return new FileSystemSnapshot(
                revision,
                new Dictionary<string, IFileInfo>(FileProviderPathUtils.PathComparer),
                new Dictionary<string, InMemoryDirectoryContents>(FileProviderPathUtils.PathComparer)
                {
                    [string.Empty] = rootDirectory,
                });
        }

        public FileSystemSnapshot(int revision, IReadOnlyDictionary<string, IFileInfo> filesByPath, IReadOnlyDictionary<string, InMemoryDirectoryContents> directoriesByPath)
        {
            Revision = revision;
            FilesByPath = filesByPath;
            DirectoriesByPath = directoriesByPath;
        }

        public int Revision { get; }

        public IReadOnlyDictionary<string, IFileInfo> FilesByPath { get; }

        public IReadOnlyDictionary<string, InMemoryDirectoryContents> DirectoriesByPath { get; }
    }

    private sealed class InMemoryDirectoryEntryInfo : IFileInfo, IFileProviderPathInfo
    {
        private readonly IFileInfo source;

        public InMemoryDirectoryEntryInfo(string subpath, IFileInfo source, DateTimeOffset lastModified)
        {
            Subpath = FileProviderPathUtils.NormalizeProviderPath(subpath);
            this.source = source;
            LastModified = source != null ? GetLastModified(source) : lastModified;
        }

        public string Subpath { get; }

        public Stream CreateReadStream()
        {
            throw new InvalidOperationException($"Cannot create a stream for a directory entry '{Subpath}'.");
        }

        public bool Exists => true;

        public bool IsDirectory => true;

        public DateTimeOffset LastModified { get; }

        public long Length => -1;

        public string Name => FileProviderPathUtils.GetLeafName(Subpath);

        public string PhysicalPath => source?.PhysicalPath;
    }

    private sealed class InMemoryDirectoryContents : IDirectoryContents
    {
        private IFileInfo[] entries = Array.Empty<IFileInfo>();

        public bool Exists => true;

        public IEnumerator<IFileInfo> GetEnumerator()
        {
            return ((IEnumerable<IFileInfo>) entries).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void SetEntries(IFileInfo[] entries)
        {
            this.entries = entries ?? Array.Empty<IFileInfo>();
        }
    }

}
