namespace PoeShared.Scaffolding;

public static class DirectoryInfoExtensions
{
    public static bool IsParentOf(this DirectoryInfo parentDir, DirectoryInfo candidatePath)
    {
        return PathUtils.IsParentDir(parentDir.FullName, candidatePath.FullName);
    }
    
    public static bool IsDirOrSubDir(this DirectoryInfo parentDir, DirectoryInfo candidatePath)
    {
        return PathUtils.IsDirOrSubDir(parentDir.FullName, candidatePath.FullName);
    }
    
    public static bool IsSubDir(this DirectoryInfo parentDir, DirectoryInfo candidatePath)
    {
        return PathUtils.IsSubDir(parentDir.FullName, candidatePath.FullName);
    }
}