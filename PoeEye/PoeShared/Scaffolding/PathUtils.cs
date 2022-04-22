﻿using Meziantou.Framework;

namespace PoeShared.Scaffolding;

public static class PathUtils
{
    public static string GetCommonRootDirectory(IReadOnlyList<string> paths)
    {
        return GetCommonRootDirectory(paths, Path.DirectorySeparatorChar);
    }
    
    public static string GetCommonRootDirectory(IReadOnlyList<string> paths, char separator)
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

    public static bool IsParentDir(string candidatePath, string fullPath)
    {
        if (candidatePath == fullPath || string.IsNullOrEmpty(candidatePath) || string.IsNullOrEmpty(fullPath))
        {
            return false;
        }
        
        var path1 = FullPath.FromPath(candidatePath);
        var path2 = FullPath.FromPath(fullPath);
        return path2.IsChildOf(path1);
    }
    
    public static bool IsSubDir(string candidatePath, string fullPath)
    {
        if (candidatePath == fullPath || string.IsNullOrEmpty(candidatePath) || string.IsNullOrEmpty(fullPath))
        {
            return false;
        }

        var separatorIdx = fullPath.IndexOf(Path.DirectorySeparatorChar);
        var subfolderIdx = separatorIdx + 1;
        if (separatorIdx <= 0 || subfolderIdx == fullPath.Length)
        {
            // provided path does not have subdirectories
            return false;
        }

        var subPath = fullPath[subfolderIdx..];
        var path1 = FullPath.FromPath(candidatePath);
        var path2 = FullPath.FromPath(subPath);
        return path1 == path2 || path2.IsChildOf(path1);
    }
    
    public static bool IsDirOrSubDir(string candidatePath, string fullPath)
    {
        if (candidatePath == fullPath)
        {
            return true;
        }
        
        if (string.IsNullOrEmpty(candidatePath) || string.IsNullOrEmpty(fullPath))
        {
            return false;
        }

        var path1 = FullPath.FromPath(candidatePath);
        var path2 = FullPath.FromPath(fullPath);
        return path2.IsChildOf(path1);
    }
}