using Microsoft.Extensions.FileProviders;
using PoeShared.IO;

namespace PoeShared.Scaffolding;

/// <summary>
/// Exposes the provider-relative subpath for <see cref="IFileInfo"/> implementations.
/// </summary>
public interface IFileProviderPathInfo
{
    string Subpath { get; }
}

internal static class FileProviderPathUtils
{
    private static readonly char[] DirectorySeparators = ['/', '\\'];
    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

    public static StringComparer PathComparer { get; } = PathUtils.IsWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    public static bool TryNormalizeSubpath(string subpath, out string normalizedSubpath)
    {
        normalizedSubpath = string.Empty;

        if (subpath == null)
        {
            return false;
        }

        if (!TryDecodeSubpath(subpath, out var decodedSubpath))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(decodedSubpath))
        {
            return true;
        }

        if (IsForbiddenRootedPath(decodedSubpath))
        {
            return false;
        }

        decodedSubpath = decodedSubpath.TrimStart(DirectorySeparators);
        if (string.IsNullOrWhiteSpace(decodedSubpath))
        {
            return true;
        }

        if (IsDotOnlyPath(decodedSubpath))
        {
            return false;
        }

        var segments = new List<string>();
        foreach (var segment in decodedSubpath.Split(DirectorySeparators, StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == ".")
            {
                continue;
            }

            if (segment == "..")
            {
                if (segments.Count == 0)
                {
                    return false;
                }

                segments.RemoveAt(segments.Count - 1);
                continue;
            }

            if (segment.IndexOfAny(InvalidFileNameChars) >= 0)
            {
                return false;
            }

            segments.Add(segment);
        }

        normalizedSubpath = segments.Count == 0 ? string.Empty : string.Join(Path.DirectorySeparatorChar, segments);
        return true;
    }

    public static string NormalizeStoragePath(string subpath)
    {
        return TryNormalizeSubpath(subpath, out var normalizedSubpath)
            ? normalizedSubpath
            : NormalizeRawPath(subpath);
    }

    public static string NormalizeProviderPath(string subpath)
    {
        return NormalizeStoragePath(subpath).Replace('\\', '/');
    }

    public static string ToStorageKey(string subpath)
    {
        var normalizedSubpath = NormalizeStoragePath(subpath);
        return PathUtils.IsWindows ? normalizedSubpath.ToLowerInvariant() : normalizedSubpath;
    }

    public static string GetSubpath(IFileInfo fileInfo)
    {
        return fileInfo switch
        {
            IFileProviderPathInfo pathAwareFileInfo => pathAwareFileInfo.Subpath,
            null => string.Empty,
            _ => NormalizeProviderPath(fileInfo.Name),
        };
    }

    public static string Combine(string parentSubpath, string entryName)
    {
        if (string.IsNullOrEmpty(parentSubpath))
        {
            return NormalizeStoragePath(entryName);
        }

        return NormalizeStoragePath(Path.Combine(parentSubpath, entryName));
    }

    public static string GetParentSubpath(string subpath)
    {
        if (string.IsNullOrEmpty(subpath))
        {
            return string.Empty;
        }

        var directoryName = Path.GetDirectoryName(subpath);
        return string.IsNullOrEmpty(directoryName) ? string.Empty : NormalizeStoragePath(directoryName);
    }

    public static string GetLeafName(string subpath)
    {
        return string.IsNullOrEmpty(subpath) ? string.Empty : Path.GetFileName(subpath);
    }

    private static string NormalizeRawPath(string subpath)
    {
        if (string.IsNullOrEmpty(subpath))
        {
            return string.Empty;
        }

        return new OSPath(subpath.TrimStart(DirectorySeparators)).FullName;
    }

    private static bool IsForbiddenRootedPath(string subpath)
    {
        if (string.IsNullOrEmpty(subpath))
        {
            return false;
        }

        if ((subpath.StartsWith(@"\\", StringComparison.Ordinal) || subpath.StartsWith("//", StringComparison.Ordinal)) &&
            !string.IsNullOrEmpty(subpath.Trim(DirectorySeparators)))
        {
            return true;
        }

        return subpath.Length >= 2 && subpath[1] == ':' && char.IsLetter(subpath[0]);
    }

    private static bool IsDotOnlyPath(string subpath)
    {
        foreach (var segment in subpath.Split(DirectorySeparators, StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment != ".")
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryDecodeSubpath(string subpath, out string decodedSubpath)
    {
        decodedSubpath = subpath;
        if (subpath.IndexOf('%') < 0)
        {
            return true;
        }

        try
        {
            decodedSubpath = Uri.UnescapeDataString(subpath);
            return true;
        }
        catch (UriFormatException)
        {
            decodedSubpath = string.Empty;
            return false;
        }
    }
}

internal sealed class PathAwareFileInfo : IFileInfo, IFileProviderPathInfo
{
    private readonly IFileInfo source;

    public PathAwareFileInfo(string subpath, IFileInfo source)
    {
        Subpath = FileProviderPathUtils.NormalizeProviderPath(subpath);
        this.source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public string Subpath { get; }

    public Stream CreateReadStream()
    {
        return source.CreateReadStream();
    }

    public bool Exists => source.Exists;

    public bool IsDirectory => source.IsDirectory;

    public DateTimeOffset LastModified => source.LastModified;

    public long Length => source.Length;

    public string Name => FileProviderPathUtils.GetLeafName(Subpath);

    public string PhysicalPath => source.PhysicalPath;
}

internal sealed class PathAwareNotFoundFileInfo : IFileInfo, IFileProviderPathInfo
{
    public PathAwareNotFoundFileInfo(string subpath)
    {
        Subpath = FileProviderPathUtils.NormalizeProviderPath(subpath);
    }

    public string Subpath { get; }

    public Stream CreateReadStream()
    {
        throw new FileNotFoundException($"The file '{Subpath}' was not found.", Name);
    }

    public bool Exists => false;

    public bool IsDirectory => false;

    public DateTimeOffset LastModified => DateTimeOffset.MinValue;

    public long Length => -1;

    public string Name => FileProviderPathUtils.GetLeafName(Subpath);

    public string PhysicalPath => null;
}
