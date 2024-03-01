using Newtonsoft.Json;

namespace PoeShared.IO;

/// <summary>
/// Represents a platform-agnostic file system path. This record normalizes input paths
/// based on the operating system, providing utility methods to handle file paths
/// in a cross-platform manner.
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
    /// Gets the platform-specific full path.
    /// </summary>
    public string FullPath { get; }

    /// <summary>
    /// Gets the file name component of the path.
    /// </summary>
    public string FileName => Path.GetFileName(FullPath);

    /// <summary>
    /// Gets the directory name component of the path.
    /// </summary>
    public string DirectoryName => Path.GetDirectoryName(FullPath);

    /// <summary>
    /// Gets a value indicating whether the file denoted by this path exists.
    /// </summary>
    public bool Exists => File.Exists(FullPath);

    /// <summary>
    /// Gets the depth of the path, calculated as the number of directory levels in the path.
    /// </summary>
    public int Depth => PathUtils.GetDepth(FullPath);

    /// <summary>
    /// Gets the Windows-style path representation.
    /// </summary>
    public string AsWindowsPath => ToWindowsPath(FullPath);

    /// <summary>
    /// Gets the Unix-style path representation.
    /// </summary>
    public string AsUnixPath => ToUnixPath(FullPath);
    
    public static implicit operator OSPath(string path) => new(path);

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

    internal static string ToWindowsPath(string path)
    {
        return path.Replace(UnixDirectorySeparator, WindowsDirectorySeparator).TrimEnd(AllSeparators);
    }

    internal static string ToUnixPath(string path)
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