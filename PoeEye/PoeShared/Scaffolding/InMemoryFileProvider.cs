using System.Web;
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
    private readonly ISourceCache<IFileInfo, OSPath> filesByName = new SourceCache<IFileInfo, OSPath>(x => new OSPath(x.Name));

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryFileProvider"/> class.
    /// </summary>
    public InMemoryFileProvider()
    {
        filesByName.CountChanged.Subscribe(x => Count = x).AddTo(Anchors);
    }

    /// <inheritdoc />
    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        var decodedSubpath = HttpUtility.UrlDecode(subpath).TrimStart('/');

        if (string.IsNullOrEmpty(decodedSubpath) || decodedSubpath == ".")
        {
            return new InMemoryDirectoryContents(filesByName.Items);
        }
        
        var matchingFiles = filesByName.Items
            .Where(file => PathUtils.IsSubDir(decodedSubpath, file.Name))
            .ToList();

        return matchingFiles.Any()
            ? new InMemoryDirectoryContents(matchingFiles)
            : NotFoundDirectoryContents.Singleton;
    }

    /// <inheritdoc />
    public IFileInfo GetFileInfo(string subpath)
    {
        // Decode the URL-encoded subpath
        var decodedSubpath = HttpUtility.UrlDecode(subpath);

        if (filesByName.TryGetValue(new OSPath(decodedSubpath), out var fileInfo))
        {
            return fileInfo;
        }
        return new NotFoundFileInfo(decodedSubpath);
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
    public ISourceCache<IFileInfo, OSPath> FilesByName => filesByName;

    /// <inheritdoc />
    public IObservable<IChangeSet<IFileInfo, OSPath>> Connect(Func<IFileInfo, bool> predicate = null, bool suppressEmptyChangeSets = true)
    {
        return filesByName.Connect(predicate, suppressEmptyChangeSets);
    }

    /// <inheritdoc />
    public IObservable<IChangeSet<IFileInfo, OSPath>> Preview(Func<IFileInfo, bool> predicate = null)
    {
        return filesByName.Preview(predicate);
    }

    /// <inheritdoc />
    public IObservable<Change<IFileInfo, OSPath>> Watch(OSPath key)
    {
        return filesByName.Watch(key);
    }

    /// <inheritdoc />
    public IObservable<int> CountChanged => filesByName.CountChanged;

    /// <inheritdoc />
    public Optional<IFileInfo> Lookup(OSPath key)
    {
        return filesByName.Lookup(key);
    }

    /// <inheritdoc />
    public IEnumerable<IFileInfo> Items => filesByName.Items;

    /// <inheritdoc />
    public IEnumerable<OSPath> Keys => filesByName.Keys;

    /// <inheritdoc />
    public IEnumerable<KeyValuePair<OSPath, IFileInfo>> KeyValues => filesByName.KeyValues;

    /// <inheritdoc />
    public void Edit(Action<ISourceUpdater<IFileInfo, OSPath>> updateAction)
    {
        filesByName.Edit(updateAction);
    }

    /// <inheritdoc />
    public Func<IFileInfo, OSPath> KeySelector => filesByName.KeySelector;

    /// <summary>
    /// Represents a directory's contents stored in memory.
    /// </summary>
    private sealed class InMemoryDirectoryContents : IDirectoryContents
    {
        private readonly List<IFileInfo> files;

        public InMemoryDirectoryContents(IEnumerable<IFileInfo> files)
        {
            this.files = files.ToList();
        }

        /// <inheritdoc />
        public bool Exists => true;

        /// <inheritdoc />
        public IEnumerator<IFileInfo> GetEnumerator()
        {
            return files.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
