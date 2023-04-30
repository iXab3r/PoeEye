using Newtonsoft.Json;

namespace PoeShared.IO;

/// <summary>
/// Platform-specific OS path, will normalize all input paths
/// </summary>
[JsonConverter(typeof(OSPathConverter))]
public record OSPath
{
    private static readonly char WindowsDirectorySeparator = '\\';
    private static readonly char UnixDirectorySeparator = '/';
    private static readonly char[] AllSeparators = { WindowsDirectorySeparator, UnixDirectorySeparator };
    
    public OSPath(string fullPath) : this (fullPath, PathUtils.IsWindows)
    {
    }

    private OSPath(string fullPath, bool isWindows)
    {
        FullPath = ToPlatformSpecificPath(fullPath, isWindows);
    }

    /// <summary>
    /// Platform-specific full path
    /// </summary>
    public string FullPath { get; }

    public string FileName => Path.GetFileName(FullPath);

    public string DirectoryName => Path.GetDirectoryName(FullPath);

    public bool Exists => File.Exists(FullPath);

    public int Depth => PathUtils.GetDepth(FullPath);

    public string AsWindowsPath => ToWindowsPath(FullPath);

    public string AsUnixPath => ToUnixPath(FullPath);

    public OSPath Combine(string other)
    {
        return Combine(new OSPath(other));
    }
    
    public OSPath Combine(OSPath other)
    {
        return new OSPath(Path.Combine(FullPath, other.FullPath));
    }

    public virtual bool Equals(OSPath other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return string.Equals(FullPath, other.FullPath, PathUtils.IsWindows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    public override int GetHashCode()
    {
        return (FullPath != null ? FullPath.GetHashCode() : 0);
    }

    private static string ToWindowsPath(string path)
    {
        return path.Replace(UnixDirectorySeparator, WindowsDirectorySeparator).TrimEnd(AllSeparators);
    }

    private static string ToUnixPath(string path)
    {
        //FIXME Most corner-cases are not covered by this simple replacement, e.g. drive path, device path
        return path.Replace(WindowsDirectorySeparator, UnixDirectorySeparator).TrimEnd(AllSeparators);
    }

    private static string ToPlatformSpecificPath(string path, bool isWindows)
    {
        if (path == null)
        {
            return string.Empty;
        }
        return isWindows ? ToWindowsPath(path) : ToUnixPath(path);
    }
}