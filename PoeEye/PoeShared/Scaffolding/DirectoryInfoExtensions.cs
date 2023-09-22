namespace PoeShared.Scaffolding;

public static class DirectoryInfoExtensions
{
    private static readonly IFluentLog Log = typeof(DirectoryInfoExtensions).PrepareLogger();

    /// <summary>
    /// Checks if a given path is a parent directory of another.
    /// </summary>
    /// <param name="candidatePath">Potential child directory path.</param>
    /// <param name="parentDir">Potential parent directory path.</param>
    /// <returns>Boolean value indicating whether the candidatePath is a child of parentDir.</returns>
    /// <example>
    /// <code>
    /// IsParentDir("C:\\temp\\file", "C:\\temp"); //Returns true
    /// </code>
    /// </example>
    public static bool IsParentOf(this DirectoryInfo parentDir, DirectoryInfo candidatePath)
    {
        return PathUtils.IsParentDir(parentDir.FullName, candidatePath.FullName);
    }
    
    /// <summary>
    /// Checks if a given path is a directory or a subdirectory of another.
    /// </summary>
    /// <param name="candidatePath">Potential subdirectory path.</param>
    /// <param name="parentDir">Parent directory path.</param>
    /// <returns>Boolean value indicating whether the candidate is a subdirectory of parentDir, or is the same as parentDir.</returns>
    /// <example>
    /// <code>
    /// IsDirOrSubDir("C:\\temp", "C:\\"); //Returns true
    /// </code>
    /// </example>
    public static bool IsDirOrSubDir(this DirectoryInfo parentDir, DirectoryInfo candidatePath)
    {
        return PathUtils.IsDirOrSubDir(parentDir.FullName, candidatePath.FullName);
    }
    
    /// <summary>
    /// Determines if a given path is a subdirectory of another path.
    /// </summary>
    /// <param name="candidatePath">The path to test for subdirectory status.</param>
    /// <param name="parentDir">The parent directory path.</param>
    /// <returns>A boolean representing whether the path is a subdirectory of the parent directory.</returns>
    /// <example>
    /// <code>
    /// IsSubDir("C:\\Program Files\\Common Files", "C:\\Program Files"); //Returns true
    /// </code>
    /// </example>
    public static bool IsSubDir(this DirectoryInfo parentDir, DirectoryInfo candidatePath)
    {
        return PathUtils.IsSubDir(parentDir.FullName, candidatePath.FullName);
    }

    /// <summary>
    /// Recursively retrieves files from the specified directory and its subdirectories
    /// matching a given search pattern. This method will skip any inaccessible directories 
    /// due to permission issues and continue with the next accessible directory.
    /// </summary>
    /// <param name="directory">The directory from which to start the search.</param>
    /// <param name="searchPattern">The search string to match against the names of files in the directory.</param>
    /// <param name="searchOption"></param>
    /// <returns>A list of <see cref="FileInfo"/> objects representing the files found that match the search pattern.</returns>
    public static IReadOnlyList<FileInfo> GetFilesSafe(this DirectoryInfo directory, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        if (!directory.Exists)
        {
            return Array.Empty<FileInfo>();
        }
        
        var result = new List<FileInfo>();
        try
        {
            var files = directory.GetFiles(searchPattern, SearchOption.TopDirectoryOnly);
            result.AddRange(files);
        }
        catch (UnauthorizedAccessException ex) 
        {
            Log.Warn($"Failed to access files in directory {directory.FullName}", ex);
        }

        if (searchOption == SearchOption.AllDirectories)
        {
            try
            {
                var subDirs = directory.GetDirectories("*", SearchOption.TopDirectoryOnly);
                foreach (var dirInfo in subDirs)
                {
                    if (!dirInfo.Exists)
                    {
                        continue;
                    }
                    var subdirFiles = dirInfo.GetFilesSafe(searchPattern, searchOption);
                    result.AddRange(subdirFiles);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Warn($"Failed to access subdirectories in directory {directory.FullName}", ex);
            }
        }

        return result;
    }
}