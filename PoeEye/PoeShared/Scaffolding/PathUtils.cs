using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Meziantou.Framework;
using PoeShared.IO;

namespace PoeShared.Scaffolding;

public static class PathUtils
{
    private static readonly Func<string, string> PathConverter;
    private static readonly Regex GenerateNameRegex = new("(?<fileName>.*?)(?<suffix> - Copy \\((?<idx>\\d+)\\))?(?<ext>\\.[\\.\\w]+)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    private static readonly ImmutableHashSet<char> InvalidChars = Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).ToImmutableHashSet();
    private static readonly ImmutableHashSet<char> FileNameReplacementChars = InvalidChars
        .Add(Path.AltDirectorySeparatorChar)
        .Add(Path.DirectorySeparatorChar);

    private static readonly char[] DirectorySeparators = new[] {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar};
    private static readonly ImmutableHashSet<string> InvalidFileNameParts;

    private static readonly ImmutableHashSet<string> ReservedFileNames = ImmutableHashSet<string>.Empty
        .WithComparer(StringComparer.OrdinalIgnoreCase)
        .Union(new[]
        {
            "CON",
            "PRN",
            "AUX",
            "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        });


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
        
        var invalidFileNameChars =  Path.GetInvalidFileNameChars();
        InvalidFileNameParts = invalidFileNameChars
            .ToImmutableHashSet()
            .Add(Path.PathSeparator)
            .Add(Path.VolumeSeparatorChar)
            .Add(Path.PathSeparator)
            .Add(Path.AltDirectorySeparatorChar)
            .Select(x => x.ToString())
            .ToImmutableHashSet()
            .Add("..");
    }

    public static bool IsWindows { get; }

    public static bool IsLinux { get; }

    /// <summary>
    /// Sanitizes a filename by replacing invalid characters with a specified replacement string.
    /// </summary>
    /// <param name="input">The original filename to sanitize. This cannot be null.</param>
    /// <param name="replacement">The string to replace invalid characters with. Defaults to "_".</param>
    /// <returns>A sanitized version of the filename where all invalid characters are replaced by the specified replacement string.</returns>
    /// <remarks>
    /// This method uses the <c>FileNameParser.Replace</c> method to process the input name, assuming this method is defined to handle the replacement of characters that are not allowed in file names. 
    /// The method should ensure that the returned file name does not contain characters that would be invalid in a file system context, such as <c>: \ / | ? *</c> among others.
    /// <para>
    /// Example:
    /// <code>
    /// string safeFileName = MakeValidFileName("example?filename<>.txt");
    /// // safeFileName would be "example_filename__.txt" if the invalid characters "?" and "<>" are replaced by "_".
    /// </code>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="input"/> parameter is null.</exception>
    public static string MakeValidFileName([NotNull] string input, [NotNull] char? replacement = '_')
    {
        var sb = new StringBuilder(input.Length);
        var changed = false;
        foreach (var c in input)
        {
            if (FileNameReplacementChars.Contains(c)) {
                changed = true;
                var repl = c switch
                {
                    _ => replacement ?? '\0'
                };

                if (repl != '\0')
                {
                    sb.Append(repl);
                }
            }
            else
            {
                sb.Append(c);
            }
        }

        if (sb.Length == 0)
        {
            return "_";
        }
        return changed ? sb.ToString() : input;
    }

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
    ///     Gets the longest common path for the paths provided.
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
        foreach (var directorySeparator in DirectorySeparators)
        {
            var path = FindLongestCommonPath(paths, directorySeparator);
            if (!string.IsNullOrEmpty(path))
            {
                return path;
            }
        }

        throw new ArgumentException($"Failed to find common path: {paths.DumpToString()}");
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
    public static string FindLongestCommonPath(IReadOnlyList<string> paths)
    {
        foreach (var directorySeparator in DirectorySeparators)
        {
            var path = FindLongestCommonPath(paths, directorySeparator);
            if (!string.IsNullOrEmpty(path))
            {
                return path;
            } 
        }

        return string.Empty;
    }

    /// <summary>
    ///     Gets the longest common path for the paths provided.
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

        var commonPath = FindLongestCommonPath(paths, separator);
        if (string.IsNullOrEmpty(commonPath))
        {
            throw new ArgumentException($"Failed to find common path: {paths.DumpToString()}");
        }

        return commonPath;
    }

    /// <summary>
    ///     Finds the longest common path for the paths provided.
    /// </summary>
    /// <param name="paths">A read-only list of path strings.</param>
    /// <param name="separator">The character used as a directory separator in the paths.</param>
    /// <returns>The common path as a string or null if path not found</returns>
    /// <example>
    ///     <code>
    /// GetLongestCommonPath(new List<string>
    ///             { "C:\\Program Files\\Common Files", "C:\\Program Files\\Uninstall Information"
    ///             }, '\\'); //Returns "C:\\Program Files"
    /// </code>
    /// </example>
    public static string FindLongestCommonPath(IReadOnlyList<string> paths, char separator)
    {
        var commonPath = string.Empty;
        var separatedPath = paths
            .First(str => str.Length == paths.Max(st2 => st2.Length))
            .Split(separator, StringSplitOptions.RemoveEmptyEntries);

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

        return string.IsNullOrEmpty(commonPath) ? null : commonPath;
    }

    /// <summary>
    /// Removes known extensions from the given file path until an unknown extension is encountered.
    /// </summary>
    /// <param name="path">The file path from which to remove extensions.</param>
    /// <param name="knownExtensions">A set of known extensions that should be removed from the path.</param>
    /// <returns>
    /// The file path without the known extensions. If no known extensions are found, the original path is returned.
    /// </returns>
    /// <remarks>
    /// This method iteratively removes the extensions from the end of the file path as long as the extensions are present in the provided set of known extensions.
    /// If an extension is encountered that is not in the set of known extensions, the method stops and returns the current path.
    /// </remarks>
    /// <example>
    /// <code>
    /// ISet<string> knownExtensions = new HashSet<string> { ".txt", ".log" };
    /// string path = "example.archive.log";
    /// string result = RemoveExtensions(path, knownExtensions);
    /// // result is "example.archive"
    /// </code>
    /// </example>
    public static string RemoveExtensions(string path, ISet<string> knownExtensions)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }
        
        var currentPath = path;
        
        while (true)
        {
            var isMatch = false;
            foreach (var extension in knownExtensions)
            {
                if (currentPath.EndsWith(extension))
                {
                    currentPath = currentPath.Substring(0, currentPath.Length - extension.Length);
                    isMatch = true;
                    break;
                }
            }

            if (!isMatch)
            {
                break;
            }
        }

        return currentPath;
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

        var separatorIdx = path.IndexOfAny(DirectorySeparators);
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
    public static bool IsParentDir(string parentDir, string candidatePath)
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
    /// <param name="startIdx">Start index that will be used for mutations</param>
    /// <returns>A valid path based on the baseName.</returns>
    /// <exception cref="ArgumentException">Thrown when no folder path is specified or invalid new folder path is provided.</exception>
    public static string GenerateValidName(
        string baseName,
        Func<string, int, string> mutation,
        Predicate<string> pathValidator,
        int startIdx = 1)
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
        return GenerateValidName((idx) =>
        {
            var tempName = idx == 1 ? candidateName : mutation(candidateName, idx);
            var fullPath = Path.Combine(folderPath, tempName + extension);
            return fullPath;
        }, pathValidator, startIdx);
    }

    public static string GenerateValidName(
        Func<int, string> generator,
        Predicate<string> pathValidator,
        int startIdx = 1)
    {
        var idx = startIdx;
        while (true)
        {
            var fullPath = generator(idx);
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
    /// <param name="filePath">The base path.</param>
    /// <param name="pathValidator">A function to validate a path, which returns true when valid and false otherwise.</param>
    /// <returns>A valid path converted from the base path.</returns>
    /// <example>
    ///     <code>
    /// GenerateValidName("C:\\temp", path => !Directory.Exists(path)); //Returns "C:\\temp (1)" if "C:\\temp" exists
    /// </code>
    /// </example>
    public static string GenerateValidName(string filePath, Predicate<string> pathValidator)
    {
        return GenerateValidName(filePath, pathValidator, idx => $" - Copy ({idx})");
    }

    /// <summary>
    ///     Creates a valid path name by appending a number to a base path name.
    /// </summary>
    /// <param name="filePath">The base path.</param>
    /// <param name="pathValidator">A function to validate a path, which returns true when valid and false otherwise.</param>
    /// <param name="suffixGenerator">A function which will be used to generate post-name suffix</param>
    /// <returns>A valid path converted from the base path.</returns>
    /// <example>
    ///     <code>
    /// GenerateValidName("C:\\temp", path => !Directory.Exists(path)); //Returns "C:\\temp (1)" if "C:\\temp" exists
    /// </code>
    /// </example>
    public static string GenerateValidName(string filePath, Predicate<string> pathValidator, Func<int, string> suffixGenerator)
    {
        var folderPath = Path.GetDirectoryName(filePath) ?? string.Empty;
        var fileName = Path.GetFileName(filePath);
        
        var match = GenerateNameRegex.Match(fileName);
        if (!match.Success)
        {
            throw new NotSupportedException($"File name is not supported: {fileName}");
        }
        var startIdx = match.Success && match.Groups["idx"].Success && int.TryParse(match.Groups["idx"].Value, out var baseIdx)
            ? baseIdx
            : 1;
        
        return GenerateValidName((idx) =>
        {
            var candidateNameBuilder = new StringBuilder();
            
            candidateNameBuilder.Append(match.Groups["fileName"].Value);
            
            if (idx > 1)
            {
                var suffix = suffixGenerator(idx);
                candidateNameBuilder.Append(suffix);
            }

            if (match.Groups["ext"].Success)
            {
                candidateNameBuilder.Append(match.Groups["ext"].Value);
            }

            var candidateName = candidateNameBuilder.ToString();
            return Path.Combine(folderPath, candidateName);
        }, pathValidator, startIdx);
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
        return GenerateValidName(baseName.FullName, (candidateName, idx) => $"{candidateName} ({idx})", x => pathValidator(new OSPath(x)));
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

    /// <summary>
    /// Checks if a given file name is a reserved name in Windows.
    /// </summary>
    /// <param name="fileName">The file name to check.</param>
    /// <returns>
    /// <c>true</c> if the file name is reserved by the Windows operating system (e.g., "CON", "PRN", "AUX", etc.);
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Windows reserves certain file names that cannot be used as regular file or directory names. This method
    /// checks if the provided file name is one of those reserved names. The check is case-insensitive.
    /// </remarks>
    public static bool IsReserved(string fileName)
    {
        return ReservedFileNames.Contains(fileName);
    }
    
    /// <summary>
    /// Strips invalid characters from a given file name.
    /// </summary>
    /// <param name="fileName">The file name from which to remove invalid characters.</param>
    /// <returns>A string with all invalid file name characters removed.</returns>
    public static string StripInvalidFileNameCharacters(string fileName)
    {
        var result = new StringBuilder(fileName);

        foreach (var namePart in InvalidFileNameParts)
        {
            result.Replace(namePart, string.Empty);
        }
        
        return result.ToString();
    }
    
    /// <summary>
    /// Checks if a given file name is valid according to Windows file name rules.
    /// </summary>
    /// <param name="fileName">The file name to validate.</param>
    /// <returns>An <see cref="AnnotatedBoolean"/> indicating whether the file name is valid and, if not, 
    /// which invalid character or part is present in the file name.</returns>
    public static AnnotatedBoolean IsValidWindowsFileName([NotNull] string fileName)
    {
        if (fileName == null)
        {
            throw new ArgumentNullException(nameof(fileName));
        }

        if (string.IsNullOrEmpty(fileName))
        {
            return new AnnotatedBoolean(true);
        }

        if (IsReserved(fileName))
        {
            return new AnnotatedBoolean(false, $"\"{fileName}\" is reserved for system needs");
        }

        var invalidPart = InvalidFileNameParts.FirstOrDefault(fileName.Contains);
        if (invalidPart != null)
        {
            return new AnnotatedBoolean(false, $"\"{fileName}\" contains invalid file name char '{invalidPart}'");
        }
        
        return new AnnotatedBoolean(true);
    }
}