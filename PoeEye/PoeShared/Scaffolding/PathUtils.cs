using Meziantou.Framework;

namespace PoeShared.Scaffolding;

public static class PathUtils
{
    public static bool IsSubpath(string path, string fullPath)
    {
        if (path == fullPath)
        {
            return true;
        }

        if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(fullPath))
        {
            return false;
        }

        var path1 = FullPath.FromPath(path ?? string.Empty);
        var path2 = FullPath.FromPath(fullPath ?? string.Empty);

        return path2.IsChildOf(path1);
    }
}