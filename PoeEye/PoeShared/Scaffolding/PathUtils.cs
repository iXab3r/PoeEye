using JetBrains.Annotations;
using Meziantou.Framework;
using PoeShared.IO;

namespace PoeShared.Scaffolding;

public static class PathUtils
{
    private static readonly Func<string, string> PathConverter;

    static PathUtils()
    {
#if NET5_0_OR_GREATER
        IsWindows = OperatingSystem.IsWindows();
        IsLinux = OperatingSystem.IsLinux();
#else
        IsWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
        IsLinux = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);
#endif
        PathConverter = IsWindows ? x => x?.ToLower() : x => x;
    }

    public static bool IsWindows { get; }
    public static bool IsLinux { get; }

    /// <summary>
    ///     Gets the root directory for the common paths provided
    /// </summary>
    /// <param name="paths">A read-only list of path strings.</param>
    /// <returns>
    ///     The common root director as a string.
    /// </returns>
    /// <example>
    ///     <code>
    /// GetRootDirectory(new List<string> { "C:\\Program Files", "C:\\Windows" }); //Returns "C:\\"
    /// </code>
    /// </example>
    /// <exception cref="ArgumentException">Thrown when no paths or no common root is provided.</exception>
    public static string GetRootDirectory(IReadOnlyList<string> paths)
    {
        if (paths.IsEmpty())
        {
            throw new ArgumentException("At least one path must be supplied");
        }

        var roots = paths.Select(GetRootDirectory).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToArray();

        if (roots.Length == 1)
        {
            return roots[0];
        }

        if (!roots.Any())
        {
            throw new ArgumentException($"There is no common root for paths {paths.DumpToString()}");
        }

        throw new ArgumentException($"There are multiple potential roots: {roots.DumpToString()}");
    }

    /// <summary>
    ///     Adds a specified prefix to the extension of a file path.
    /// </summary>
    /// <param name="path">The original file path.</param>
    /// <param name="prefix">The prefix to append to the file extension.</param>
    /// <returns>
    ///     The file path with the prefix appended to the file extension.
    ///     If the directory of the path could not be determined, an empty string is used.
    /// </returns>
    /// <example>
    ///     <code>
    /// AddExtensionPrefix("C:\\temp\\file.txt", "bak"); // Returns "C:\\temp\\file.bak.txt"
    /// </code>
    /// </example>
    public static string AddExtensionPrefix(string path, string prefix)
    {
        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        return Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(path)}.{prefix}{Path.GetExtension(path)}");
    }

    /// <summary>
    ///     Finds the longest common path for the paths provided.
    /// </summary>
    /// <param name="paths">A read-only list of path strings.</param>
    /// <returns>The common path as a string</returns>
    /// <example>
    ///     <code>
    /// GetLongestCommonPath(new List<string>
    ///             { "C:\\Program Files\\Common Files", "C:\\Program Files\\Uninstall Information"
    ///             }); //Returns "C:\\Program Files"
    /// </code>
    /// </example>
    public static string GetLongestCommonPath(IReadOnlyList<string> paths)
    {
        return GetLongestCommonPath(paths, Path.DirectorySeparatorChar);
    }

    /// <summary>
    ///     Finds the longest common path for the paths provided.
    /// </summary>
    /// <param name="paths">A read-only list of path strings.</param>
    /// <param name="separator">The character used as a directory separator in the paths.</param>
    /// <returns>The common path as a string</returns>
    /// <example>
    ///     <code>
    /// GetLongestCommonPath(new List<string>
    ///             { "C:\\Program Files\\Common Files", "C:\\Program Files\\Uninstall Information"
    ///             }, '\\'); //Returns "C:\\Program Files"
    /// </code>
    /// </example>
    public static string GetLongestCommonPath(IReadOnlyList<string> paths, char separator)
    {
        if (paths.IsEmpty())
        {
            throw new ArgumentException("At least one path must be supplied");
        }

        var commonPath = string.Empty;
        var separatedPath = paths
            .First(str => str.Length == paths.Max(st2 => st2.Length))
            .Split(separator, StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        foreach (var segment in separatedPath.AsEnumerable())
        {
            if (commonPath.Length == 0 && paths.All(str => str.StartsWith(segment)))
            {
                commonPath = segment;
            }
            else if (paths.All(str => str.StartsWith(commonPath + separator + segment)))
            {
                commonPath += separator + segment;
            }
            else
            {
                break;
            }
        }

        if (string.IsNullOrEmpty(commonPath))
        {
            throw new ArgumentException($"Failed to find common path: {paths.DumpToString()}");
        }

        return commonPath;
    }

    /// <summary>
    /// Gets the file name from the specified path string without the extension.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>The file name without the extension, or null if the path is null.</returns>
    public static string GetFileNameWithoutExtension(string path)
    {
        if (path == null)
        {
            return null;
        }

        var result = GetFileNameWithoutExtension(path.AsSpan());
        if (path.Length == result.Length)
        {
            return path;
        }

        return result.ToString();
    }

    /// <summary>
    /// Returns the file name without the extension from a ReadOnlySpan<char> representing the path.
    /// This method is useful for span-based parsing to avoid string allocations.
    /// </summary>
    /// <param name="path">The file path as a ReadOnlySpan<char>.</param>
    /// <returns>A ReadOnlySpan<char> containing the file name without the extension.</returns>
    /// <remarks>
    /// This method operates on a ReadOnlySpan<char> to allow for more efficient memory usage
    /// when working with substrings. If there is no extension in the path, the method returns
    /// the file name as-is. If the path is empty or consists only of directory separators,
    /// an empty ReadOnlySpan<char> is returned.
    /// </remarks>
    public static ReadOnlySpan<char> GetFileNameWithoutExtension(ReadOnlySpan<char> path)
    {
        var fileName = Path.GetFileName(path);
        var firstPeriod = fileName.IndexOf('.');
        return firstPeriod < 0
            ? fileName
            : fileName.Slice(0, firstPeriod);
    }

    /// <summary>
    ///     Determines the root directory of a given path.
    /// </summary>
    /// <param name="path">The path for which to determine the root directory.</param>
    /// <returns>If the root path of the directory is found, it's returned as a string. If not, an empty string is returned.</returns>
    /// <example>
    ///     <code>
    /// GetRootDirectory("C:\\temp\\file.txt"); // Returns "C:\\"
    /// </code>
    /// </example>
    public static string GetRootDirectory(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        var separatorIdx = path.IndexOf(Path.DirectorySeparatorChar);
        if (separatorIdx < 0)
        {
            separatorIdx = path.IndexOf(Path.AltDirectorySeparatorChar);
        }

        return separatorIdx <= 0 ? path : path.Substring(0, separatorIdx);
    }

    /// <summary>
    ///     Calculates the depth of a directory path.
    /// </summary>
    /// <param name="path">The path for which to calculate the depth.</param>
    /// <returns>The depth of the path as an integer. Returns 0 if path is null or whitespace.</returns>
    /// <example>
    ///     <code>
    /// GetDepth("C:\\temp\\file.txt"); // Returns 2
    /// </code>
    /// </example>
    public static int GetDepth(string path)
    {
        return string.IsNullOrWhiteSpace(path) ? 0 : path.Count(x => x == Path.DirectorySeparatorChar || x == Path.AltDirectorySeparatorChar);
    }
    
    /// <summary>
    ///     Checks if the two provided paths are the same.
    /// </summary>
    /// <param name="first">First path to compare.</param>
    /// <param name="second">Second path to compare.</param>
    /// <returns>Boolean value indicating whether the two paths are the same.</returns>
    /// <example>
    ///     <code>
    /// IsSamePath("C:\\temp\\file.txt", "C:/temp/file.txt"); //Returns true
    /// </code>
    /// </example>
    public static bool IsSamePath(FileSystemInfo first, FileSystemInfo second)
    {
        return first.FullName.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).SequenceEqual(second.FullName.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }

    /// <summary>
    ///     Checks if the two provided paths are the same.
    /// </summary>
    /// <param name="first">First path to compare.</param>
    /// <param name="second">Second path to compare.</param>
    /// <returns>Boolean value indicating whether the two paths are the same.</returns>
    /// <example>
    ///     <code>
    /// IsSamePath("C:\\temp\\file.txt", "C:/temp/file.txt"); //Returns true
    /// </code>
    /// </example>
    public static bool IsSamePath(string first, string second)
    {
        return first.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).SequenceEqual(second.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }

    /// <summary>
    ///     Checks if a given path is a parent directory of another.
    /// </summary>
    /// <param name="candidatePath">Potential child directory path.</param>
    /// <param name="parentDir">Potential parent directory path.</param>
    /// <returns>Boolean value indicating whether the candidatePath is a child of parentDir.</returns>
    /// <example>
    ///     <code>
    /// IsParentDir("C:\\temp\\file", "C:\\temp"); //Returns true
    /// </code>
    /// </example>
    public static bool IsParentDir(string candidatePath, string parentDir)
    {
        if (candidatePath == parentDir || string.IsNullOrEmpty(candidatePath) || string.IsNullOrEmpty(parentDir))
        {
            return false;
        }

        var path1 = FullPath.FromPath(PathConverter(candidatePath));
        var path2 = FullPath.FromPath(PathConverter(parentDir));
        return path2.IsChildOf(path1);
    }

    /// <summary>
    ///     Determines if a given path is a subdirectory of another path.
    /// </summary>
    /// <param name="candidatePath">The path to test for subdirectory status.</param>
    /// <param name="parentDir">The parent directory path.</param>
    /// <returns>A boolean representing whether the path is a subdirectory of the parent directory.</returns>
    /// <example>
    ///     <code>
    /// IsSubDir("C:\\Program Files\\Common Files", "C:\\Program Files"); //Returns true
    /// </code>
    /// </example>
    public static bool IsSubDir(string candidatePath, string parentDir)
    {
        if (candidatePath == parentDir || string.IsNullOrEmpty(candidatePath) || string.IsNullOrEmpty(parentDir))
        {
            return false;
        }

        var separatorIdx = parentDir.IndexOf(Path.DirectorySeparatorChar);
        var subfolderIdx = separatorIdx + 1;
        if (separatorIdx <= 0 || subfolderIdx == parentDir.Length)
        {
            // provided path does not have subdirectories
            return false;
        }

        var subPath = parentDir[subfolderIdx..];
        var path1 = FullPath.FromPath(PathConverter(candidatePath));
        var path2 = FullPath.FromPath(PathConverter(subPath));
        return path1 == path2 || path2.IsChildOf(path1);
    }

    /// <summary>
    ///     Checks if a given path is a directory or a subdirectory of another.
    /// </summary>
    /// <param name="candidate">Potential subdirectory path.</param>
    /// <param name="parentDir">Parent directory path.</param>
    /// <returns>Boolean value indicating whether the candidate is a subdirectory of parentDir, or is the same as parentDir.</returns>
    /// <example>
    ///     <code>
    /// IsDirOrSubDir("C:\\temp", "C:\\"); //Returns true
    /// </code>
    /// </example>
    public static bool IsDirOrSubDir(string candidate, string parentDir)
    {
        if (candidate == parentDir)
        {
            return true;
        }

        if (string.IsNullOrEmpty(candidate) || string.IsNullOrEmpty(parentDir))
        {
            return false;
        }

        var path1 = FullPath.FromPath(PathConverter(candidate));
        var path2 = FullPath.FromPath(PathConverter(parentDir));
        return path2.IsChildOf(path1);
    }

    /// <summary>
    ///     Generates a valid path name by mutating a base path name.
    /// </summary>
    /// <param name="baseName">The base path.</param>
    /// <param name="mutation">A function defining how to mutate the baseName when the pathValidator returns false.</param>
    /// <param name="pathValidator">
    ///     A function to check the validity of a path, which returns true when valid and false
    ///     otherwise.
    /// </param>
    /// <returns>A valid path based on the baseName.</returns>
    /// <exception cref="ArgumentException">Thrown when no folder path is specified or invalid new folder path is provided.</exception>
    public static string GenerateValidName(
        string baseName,
        Func<string, int, string> mutation,
        Predicate<string> pathValidator)
    {
        if (string.IsNullOrEmpty(baseName))
        {
            throw new ArgumentException("New folder path must be specified");
        }

        var extension = Path.GetExtension(baseName);
        var candidateName = Path.GetFileNameWithoutExtension(baseName);
        if (string.IsNullOrEmpty(candidateName))
        {
            throw new ArgumentException($"Invalid new folder path: {baseName}");
        }

        var folderPath = Path.GetDirectoryName(baseName) ?? string.Empty;
        var idx = 1;
        while (true)
        {
            var tempName = idx == 1 ? candidateName : mutation(candidateName, idx);
            var fullPath = Path.Combine(folderPath, tempName + extension);
            if (!pathValidator(fullPath))
            {
                idx++;
            }
            else
            {
                return fullPath;
            }
        }
    }

    /// <summary>
    ///     Creates a valid path name by appending a number to a base path name.
    /// </summary>
    /// <param name="baseName">The base path.</param>
    /// <param name="pathValidator">A function to validate a path, which returns true when valid and false otherwise.</param>
    /// <returns>A valid path converted from the base path.</returns>
    /// <example>
    ///     <code>
    /// GenerateValidName("C:\\temp", path => !Directory.Exists(path)); //Returns "C:\\temp (1)" if "C:\\temp" exists
    /// </code>
    /// </example>
    public static string GenerateValidName(string baseName, Predicate<string> pathValidator)
    {
        return GenerateValidName(baseName, (candidateName, idx) => $"{candidateName} ({idx})", pathValidator);
    }
    
    /// <summary>
    ///     Creates a valid path name by appending a number to a base path name.
    /// </summary>
    /// <param name="baseName">The base path.</param>
    /// <param name="pathValidator">A function to validate a path, which returns true when valid and false otherwise.</param>
    /// <returns>A valid path converted from the base path.</returns>
    /// <example>
    ///     <code>
    /// GenerateValidName("C:\\temp", path => !Directory.Exists(path)); //Returns "C:\\temp (1)" if "C:\\temp" exists
    /// </code>
    /// </example>
    public static OSPath GenerateValidName(OSPath baseName, Predicate<OSPath> pathValidator)
    {
        return GenerateValidName(baseName.FullPath, (candidateName, idx) => $"{candidateName} ({idx})", x => pathValidator(new OSPath(x)));
    }

    /// <summary>
    ///     Expands a relative path to an absolute path based on a provided root.
    /// </summary>
    /// <param name="rootPath">The root path.</param>
    /// <param name="path">The relative path.</param>
    /// <returns>The expanded path.</returns>
    /// <example>
    ///     <code>
    /// ExpandPath("C:\\temp", "..\\file.txt"); //Returns "C:\\file.txt"
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when rootPath or path is null.</exception>
    /// <exception cref="FormatException">Thrown when invalid path is provided.</exception>
    public static string ExpandPath([NotNull] string rootPath, [NotNull] string path)
    {
        if (rootPath == null)
        {
            throw new ArgumentNullException(nameof(rootPath));
        }

        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        var parts = path.Split(Path.DirectorySeparatorChar);
        var result = new List<string>();

        if (!string.IsNullOrEmpty(rootPath) && (parts[0] == "." || parts[0] == ".."))
        {
            result.AddRange(rootPath.Split(Path.DirectorySeparatorChar));
        }

        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            if (part == "..")
            {
                if (result.Count > 0)
                {
                    result.RemoveAt(result.Count - 1);
                }
                else
                {
                    throw new FormatException($"Invalid path: {path}, root: {rootPath}");
                }
            }
            else if (part == ".")
            {
                //
            }
            else
            {
                result.Add(part);
            }
        }

        if (result.Count == 0)
        {
            return ".";
        }

        return result.JoinStrings(Path.DirectorySeparatorChar);
    }
}