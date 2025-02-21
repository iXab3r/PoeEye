﻿using JetBrains.Annotations;
using Newtonsoft.Json;

namespace PoeShared.IO;

/// <summary>
/// Represents a platform-agnostic file system path. This record normalizes input paths
/// based on the operating system, providing utility methods to handle file paths
/// in a cross-platform manner.
/// </summary>
[JsonConverter(typeof(OSPathConverter))]
public record OSPath : IComparable
{
    public static readonly OSPath Empty = new OSPath(string.Empty);
    
    public static readonly char WindowsDirectorySeparator = '\\';
    public static readonly char UnixDirectorySeparator = '/';
    public static readonly char[] AllSeparators = { WindowsDirectorySeparator, UnixDirectorySeparator };
    
    public OSPath(string fullPath) : this (fullPath, PathUtils.IsWindows)
    {
    }

    private OSPath(string fullPath, bool isWindows)
    {
        FullName = ToPlatformSpecificPath(fullPath, isWindows);
    }

    /// <summary>
    /// Gets the platform-specific full path.
    /// </summary>
    public string FullName { get; }

    /// <summary>
    /// Gets the file name component of the path.
    /// </summary>
    public string Name => Path.GetFileName(FullName);

    /// <summary>
    /// Gets the directory name component of the path.
    /// </summary>
    public string Directory => Path.GetDirectoryName(FullName);

    /// <summary>
    /// Gets a value indicating whether the file denoted by this path exists.
    /// </summary>
    public bool Exists => File.Exists(FullName);

    /// <summary>
    /// Gets the depth of the path, calculated as the number of directory levels in the path.
    /// </summary>
    public int Depth => PathUtils.GetDepth(FullName);

    /// <summary>
    /// Gets the Windows-style path representation.
    /// </summary>
    public string AsWindowsPath => ToWindowsPath(FullName);

    /// <summary>
    /// Gets the Unix-style path representation.
    /// </summary>
    public string AsUnixPath => ToUnixPath(FullName);
    
    public static implicit operator OSPath(string path) => new(path);

    public static bool Equals([NotNull] string path, [NotNull] string otherPath)
    {
        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (otherPath == null)
        {
            throw new ArgumentNullException(nameof(otherPath));
        }

        if (string.IsNullOrEmpty(path) && string.IsNullOrEmpty(otherPath))
        {
            return true;
        }

        return new OSPath(path).Equals(new OSPath(otherPath));
    }

    public OSPath Combine(string other)
    {
        return Combine(new OSPath(other));
    }
    
    public OSPath Combine(OSPath other)
    {
        return new OSPath(Path.Combine(FullName, other.FullName));
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

        return string.Equals(FullName, other.FullName, PathUtils.IsWindows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    public override int GetHashCode()
    {
        return (FullName != null ? FullName.GetHashCode() : 0);
    }

    public int CompareTo(object obj)
    {
        return obj switch
        {
            null => 1,
            OSPath otherPath => string.Compare(FullName, otherPath.FullName, PathUtils.IsWindows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal),
            _ => throw new ArgumentException("Object is not an OSPath")
        };
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

    public override string ToString()
    {
        return FullName;
    }
}