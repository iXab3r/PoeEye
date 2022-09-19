using System.Text;
using JetBrains.Annotations;
using Meziantou.Framework;

namespace PoeShared.Scaffolding;

public static class PathUtils
{
    private static readonly Func<string, string> PathConverter;
    private static bool IsWindows { get; }
    private static bool IsLinux { get; }

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

    public static string GetLongestCommonPath(IReadOnlyList<string> paths)
    {
        return GetLongestCommonPath(paths, Path.DirectorySeparatorChar);
    }
    
    public static string GetLongestCommonPath(IReadOnlyList<string> paths, char separator)
    {
        if (paths.IsEmpty())
        {
            throw new ArgumentException("At least one path must be supplied");
        }
        var commonPath = string.Empty;
        var separatedPath = paths
            .First ( str => str.Length == paths.Max ( st2 => st2.Length ) )
            .Split (separator, StringSplitOptions.RemoveEmptyEntries )
            .ToList ( );
 
        foreach ( var segment in separatedPath.AsEnumerable ( ) )
        {
            if ( commonPath.Length == 0 && paths.All ( str => str.StartsWith ( segment ) ) )
            {
                commonPath = segment;
            }
            else if ( paths.All ( str => str.StartsWith ( commonPath + separator + segment ) ) )
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
    
    public static string GetRootDirectory(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        var separatorIdx = path.IndexOf(Path.DirectorySeparatorChar);
        return separatorIdx <= 0 ? path : path.Substring(0, separatorIdx);
    }

    public static int GetDepth(string path)
    {
        return (path ?? string.Empty).Count(x => x == Path.DirectorySeparatorChar);
    }

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
    
    public static string GenerateValidName(string candidate, Predicate<string> pathValidator)
    {
        if (string.IsNullOrEmpty(candidate))
        {
            throw new ArgumentException($"New folder path must be specified");
        }

        var folderName = Path.GetFileNameWithoutExtension(candidate);
        if (string.IsNullOrEmpty(folderName))
        {
            throw new ArgumentException($"Invalid new folder path: {candidate}");
        }

        var folderPath = Path.GetDirectoryName(candidate) ?? string.Empty;
        var idx = 1;
        while (true)
        {
            var fullPath = Path.Combine(folderPath, idx == 1 ? folderName : $"{folderName} ({idx})");
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
            } else if (part == ".")
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