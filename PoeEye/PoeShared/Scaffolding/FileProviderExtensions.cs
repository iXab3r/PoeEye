using System.IO.Enumeration;
using Microsoft.Extensions.FileProviders;

namespace PoeShared.Scaffolding;

public static class FileProviderExtensions
{
    /// <summary>
    /// Retrieves the file information from the specified file provider or throws an exception if the file is not found.
    /// </summary>
    /// <param name="fileProvider">The file provider to query.</param>
    /// <param name="path">The path to the file within the file provider.</param>
    /// <returns>The file information of the requested file.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the specified file cannot be found.</exception>
    public static IFileInfo GetFileInfoOrThrow(this IFileProvider fileProvider, string path)
    {
        if (fileProvider == null)
        {
            throw new ArgumentNullException(nameof(fileProvider));
        }

        var file = fileProvider.GetFileInfo(path);
        if (file == null || !file.Exists || file.IsDirectory)
        {
            throw new FileNotFoundException($"Could not find file @ {path} in file provider", path);
        }

        return file;
    }

    /// <summary>
    /// Reads all bytes from a file in the specified file provider.
    /// </summary>
    /// <param name="fileProvider">The file provider to query.</param>
    /// <param name="path">The path to the file within the file provider.</param>
    /// <returns>A byte array containing the contents of the file.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the specified file cannot be found.</exception>
    /// <exception cref="IOException">Thrown if an I/O error occurs during the read operation.</exception>
    public static byte[] ReadAllBytes(this IFileProvider fileProvider, string path)
    {
        if (fileProvider == null)
        {
            throw new ArgumentNullException(nameof(fileProvider));
        }

        var file = GetFileInfoOrThrow(fileProvider, path);
        using var stream = file.CreateReadStream();
        return stream.ReadToEnd();
    }

    /// <summary>
    /// Reads all text from a file in the specified file provider.
    /// </summary>
    /// <param name="fileProvider">The file provider to query.</param>
    /// <param name="path">The path to the file within the file provider.</param>
    /// <returns>A string containing the text content of the file.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the specified file cannot be found.</exception>
    /// <exception cref="IOException">Thrown if an I/O error occurs during the read operation.</exception>
    public static string ReadAllText(this IFileProvider fileProvider, string path)
    {
        if (fileProvider == null)
        {
            throw new ArgumentNullException(nameof(fileProvider));
        }

        var file = GetFileInfoOrThrow(fileProvider, path);
        using var stream = file.CreateReadStream();
        using var streamReader = new StreamReader(stream);
        return streamReader.ReadToEnd();
    }

    /// <summary>
    /// Gets the normalized provider-relative path for a file-system entry.
    /// </summary>
    /// <param name="fileInfo">The file-system entry to inspect.</param>
    /// <returns>
    /// The provider-relative subpath when it is available, otherwise a normalized representation of <see cref="IFileInfo.Name" />.
    /// </returns>
    public static string GetSubpath(this IFileInfo fileInfo)
    {
        return FileProviderPathUtils.GetSubpath(fileInfo);
    }

    /// <summary>
    /// Retrieves files from the specified provider directory that match the supplied search pattern.
    /// </summary>
    /// <param name="fileProvider">The file provider to query.</param>
    /// <param name="subpath">The provider-relative directory to enumerate.</param>
    /// <param name="searchPattern">The file name pattern to match. Defaults to <c>*</c>.</param>
    /// <returns>An array containing the matching file entries.</returns>
    public static IFileInfo[] GetFiles(this IFileProvider fileProvider, string subpath, string searchPattern = "*")
    {
        return GetFiles(fileProvider, subpath, searchPattern, new EnumerationOptions());
    }

    /// <summary>
    /// Retrieves files from the specified provider directory that match the supplied search pattern.
    /// </summary>
    /// <param name="fileProvider">The file provider to query.</param>
    /// <param name="subpath">The provider-relative directory to enumerate.</param>
    /// <param name="searchPattern">The file name pattern to match.</param>
    /// <param name="searchOption">Determines whether only the current directory or the full subtree is searched.</param>
    /// <returns>An array containing the matching file entries.</returns>
    public static IFileInfo[] GetFiles(this IFileProvider fileProvider, string subpath, string searchPattern, SearchOption searchOption)
    {
        return GetFiles(fileProvider, subpath, searchPattern, CreateEnumerationOptions(searchOption));
    }

    /// <summary>
    /// Retrieves files from the specified provider directory that match the supplied search pattern.
    /// </summary>
    /// <param name="fileProvider">The file provider to query.</param>
    /// <param name="subpath">The provider-relative directory to enumerate.</param>
    /// <param name="searchPattern">The file name pattern to match.</param>
    /// <param name="enumerationOptions">The enumeration options that control recursion and matching behavior.</param>
    /// <returns>An array containing the matching file entries.</returns>
    public static IFileInfo[] GetFiles(this IFileProvider fileProvider, string subpath, string searchPattern, EnumerationOptions enumerationOptions)
    {
        return EnumerateEntries(fileProvider, subpath, searchPattern, enumerationOptions, includeDirectories: false).ToArray();
    }

    /// <summary>
    /// Retrieves directories from the specified provider directory that match the supplied search pattern.
    /// </summary>
    /// <param name="fileProvider">The file provider to query.</param>
    /// <param name="subpath">The provider-relative directory to enumerate.</param>
    /// <param name="searchPattern">The directory name pattern to match. Defaults to <c>*</c>.</param>
    /// <returns>An array containing the matching directory entries.</returns>
    public static IFileInfo[] GetDirectories(this IFileProvider fileProvider, string subpath, string searchPattern = "*")
    {
        return GetDirectories(fileProvider, subpath, searchPattern, new EnumerationOptions());
    }

    /// <summary>
    /// Retrieves directories from the specified provider directory that match the supplied search pattern.
    /// </summary>
    /// <param name="fileProvider">The file provider to query.</param>
    /// <param name="subpath">The provider-relative directory to enumerate.</param>
    /// <param name="searchPattern">The directory name pattern to match.</param>
    /// <param name="searchOption">Determines whether only the current directory or the full subtree is searched.</param>
    /// <returns>An array containing the matching directory entries.</returns>
    public static IFileInfo[] GetDirectories(this IFileProvider fileProvider, string subpath, string searchPattern, SearchOption searchOption)
    {
        return GetDirectories(fileProvider, subpath, searchPattern, CreateEnumerationOptions(searchOption));
    }

    /// <summary>
    /// Retrieves directories from the specified provider directory that match the supplied search pattern.
    /// </summary>
    /// <param name="fileProvider">The file provider to query.</param>
    /// <param name="subpath">The provider-relative directory to enumerate.</param>
    /// <param name="searchPattern">The directory name pattern to match.</param>
    /// <param name="enumerationOptions">The enumeration options that control recursion and matching behavior.</param>
    /// <returns>An array containing the matching directory entries.</returns>
    public static IFileInfo[] GetDirectories(this IFileProvider fileProvider, string subpath, string searchPattern, EnumerationOptions enumerationOptions)
    {
        return EnumerateEntries(fileProvider, subpath, searchPattern, enumerationOptions, includeDirectories: true).ToArray();
    }

    private static IEnumerable<IFileInfo> EnumerateEntries(IFileProvider fileProvider, string subpath, string searchPattern, EnumerationOptions enumerationOptions, bool includeDirectories)
    {
        if (fileProvider == null)
        {
            throw new ArgumentNullException(nameof(fileProvider));
        }

        if (searchPattern == null)
        {
            throw new ArgumentNullException(nameof(searchPattern));
        }

        if (enumerationOptions == null)
        {
            throw new ArgumentNullException(nameof(enumerationOptions));
        }

        if (!FileProviderPathUtils.TryNormalizeSubpath(subpath ?? string.Empty, out var normalizedSubpath))
        {
            yield break;
        }

        var preparedSearchPattern = PrepareSearchPattern(searchPattern, enumerationOptions);

        var rootDirectoryContents = TryGetDirectoryContents(fileProvider, normalizedSubpath);
        if (rootDirectoryContents == null || !TryDirectoryExists(rootDirectoryContents))
        {
            yield break;
        }

        var pendingDirectories = new Stack<PendingDirectory>();
        var visitedDirectories = new HashSet<string>(FileProviderPathUtils.PathComparer);
        pendingDirectories.Push(new PendingDirectory(normalizedSubpath, rootDirectoryContents));
        visitedDirectories.Add(normalizedSubpath);

        while (pendingDirectories.Count > 0)
        {
            var currentDirectory = pendingDirectories.Pop();
            foreach (var entry in TryEnumerate(currentDirectory.Contents))
            {
                if (!TryGetEntryName(entry, out var entryName))
                {
                    continue;
                }

                var childSubpath = FileProviderPathUtils.Combine(currentDirectory.Subpath, entryName);
                var isDirectory = TryIsDirectory(entry);
                if (isDirectory)
                {
                    var childDirectoryContents = entry as IDirectoryContents;
                    if (!TryDirectoryExists(childDirectoryContents))
                    {
                        childDirectoryContents = TryGetDirectoryContents(fileProvider, childSubpath);
                    }

                    if (!TryDirectoryExists(childDirectoryContents))
                    {
                        continue;
                    }

                    if (ShouldRecurse(enumerationOptions, entryName) && visitedDirectories.Add(childSubpath))
                    {
                        pendingDirectories.Push(new PendingDirectory(childSubpath, childDirectoryContents));
                    }

                    if (includeDirectories && IsMatch(entryName, preparedSearchPattern, enumerationOptions))
                    {
                        yield return new PathAwareFileInfo(childSubpath, entry);
                    }

                    continue;
                }

                if (!includeDirectories && IsMatch(entryName, preparedSearchPattern, enumerationOptions))
                {
                    yield return new PathAwareFileInfo(childSubpath, entry);
                }
            }
        }
    }

    private static IEnumerable<IFileInfo> TryEnumerate(IDirectoryContents directoryContents)
    {
        IEnumerator<IFileInfo> enumerator;
        try
        {
            enumerator = directoryContents.GetEnumerator();
        }
        catch
        {
            yield break;
        }

        using (enumerator)
        {
            while (true)
            {
                IFileInfo currentEntry;
                try
                {
                    if (!enumerator.MoveNext())
                    {
                        yield break;
                    }

                    currentEntry = enumerator.Current;
                }
                catch
                {
                    yield break;
                }

                if (currentEntry == null || !TryExists(currentEntry))
                {
                    continue;
                }

                yield return currentEntry;
            }
        }
    }

    private static IDirectoryContents TryGetDirectoryContents(IFileProvider fileProvider, string subpath)
    {
        try
        {
            return fileProvider.GetDirectoryContents(subpath);
        }
        catch
        {
            return null;
        }
    }

    private static EnumerationOptions CreateEnumerationOptions(SearchOption searchOption)
    {
        return new EnumerationOptions
        {
            RecurseSubdirectories = searchOption == SearchOption.AllDirectories,
        };
    }

    private static string PrepareSearchPattern(string searchPattern, EnumerationOptions enumerationOptions)
    {
        if (string.IsNullOrEmpty(searchPattern) || searchPattern == "*")
        {
            return searchPattern;
        }

        return enumerationOptions.MatchType == MatchType.Win32
            ? FileSystemName.TranslateWin32Expression(searchPattern)
            : searchPattern;
    }

    private static bool IsMatch(string entryName, string preparedSearchPattern, EnumerationOptions enumerationOptions)
    {
        if (string.IsNullOrEmpty(entryName))
        {
            return false;
        }

        if (string.IsNullOrEmpty(preparedSearchPattern) || preparedSearchPattern == "*")
        {
            return true;
        }

        var ignoreCase = enumerationOptions.MatchCasing switch
        {
            MatchCasing.CaseInsensitive => true,
            MatchCasing.CaseSensitive => false,
            _ => PathUtils.IsWindows,
        };

        return enumerationOptions.MatchType switch
        {
            MatchType.Win32 => FileSystemName.MatchesWin32Expression(preparedSearchPattern, entryName, ignoreCase),
            _ => FileSystemName.MatchesSimpleExpression(preparedSearchPattern, entryName, ignoreCase),
        };
    }

    private static bool ShouldRecurse(EnumerationOptions enumerationOptions, string entryName)
    {
        return enumerationOptions.RecurseSubdirectories &&
               (enumerationOptions.ReturnSpecialDirectories || !IsSpecialDirectory(entryName));
    }

    private static bool TryDirectoryExists(IDirectoryContents directoryContents)
    {
        if (directoryContents == null)
        {
            return false;
        }

        try
        {
            return directoryContents.Exists;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsSpecialDirectory(string entryName)
    {
        return entryName is "." or "..";
    }

    private static bool TryExists(IFileInfo entry)
    {
        try
        {
            return entry.Exists;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryIsDirectory(IFileInfo entry)
    {
        try
        {
            return entry.IsDirectory;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryGetEntryName(IFileInfo entry, out string entryName)
    {
        entryName = null;
        if (entry == null)
        {
            return false;
        }

        try
        {
            entryName = entry.Name;
            return !string.IsNullOrWhiteSpace(entryName);
        }
        catch
        {
            entryName = null;
            return false;
        }
    }

    private sealed record PendingDirectory(string Subpath, IDirectoryContents Contents);
}
