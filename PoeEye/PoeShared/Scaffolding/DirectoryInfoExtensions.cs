namespace PoeShared.Scaffolding;

public static class DirectoryInfoExtensions
{
    private static readonly IFluentLog Log = typeof(DirectoryInfoExtensions).PrepareLogger();

    /// <summary>
    /// Creates a <see cref="DirectoryInfo"/> instance representing a subdirectory within the specified parent directory.
    /// </summary>
    /// <param name="parentDir">The parent directory.</param>
    /// <param name="subdirName">The name of the subdirectory.</param>
    /// <returns>A <see cref="DirectoryInfo"/> instance for the specified subdirectory.</returns>
    public static DirectoryInfo GetSubdirectory(this DirectoryInfo parentDir, string subdirName)
    {
        return new DirectoryInfo(Path.Combine(parentDir.FullName, subdirName));
    }
    
    /// <summary>
    /// Creates a <see cref="FileInfo"/> instance for a file located in the specified directory or its subdirectories.
    /// </summary>
    /// <param name="parentDir">The parent directory to which the file path is relative.</param>
    /// <param name="relativeFilePath">The file path relative to the parent directory.</param>
    /// <returns>A <see cref="FileInfo"/> instance for the specified file.</returns>
    public static FileInfo GetFileInfo(this DirectoryInfo parentDir, string relativeFilePath)
    {
        return new FileInfo(Path.Combine(parentDir.FullName, relativeFilePath));
    }
    
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
    public static FileInfo[] GetFilesSafe(this DirectoryInfo directory, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
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

                    try
                    {
                        var subdirFiles = dirInfo.GetFilesSafe(searchPattern, searchOption);
                        result.AddRange(subdirFiles);
                    }
                    catch (DirectoryNotFoundException)
                    {
                        Log.Warn($"Virtualization error - folder seems to exist, but in fact it does not: {dirInfo.FullName}");
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Warn($"Failed to access subdirectories in directory {directory.FullName}", ex);
            }
        }

        return result.ToArray();
    }
    
     /// <summary>
    /// Recursively retrieves subfolders from the specified directory and its subdirectories
    /// matching a given search pattern. This method will skip any inaccessible directories 
    /// due to permission issues and continue with the next accessible directory.
    /// </summary>
    /// <param name="directory">The directory from which to start the search.</param>
    /// <param name="searchPattern">The search string to match against the names of files in the directory.</param>
    /// <param name="searchOption"></param>
    /// <returns>A list of <see cref="FileInfo"/> objects representing the files found that match the search pattern.</returns>
    public static DirectoryInfo[] GetDirectoriesSafe(this DirectoryInfo directory, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        if (!directory.Exists)
        {
            return Array.Empty<DirectoryInfo>();
        }
        
        var result = new List<DirectoryInfo>();
        try
        {
            var files = directory.GetDirectories(searchPattern, SearchOption.TopDirectoryOnly);
            result.AddRange(files);
        }
        catch (UnauthorizedAccessException ex) 
        {
            Log.Warn($"Failed to access subfolders in directory {directory.FullName}", ex);
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

                    try
                    {
                        var subdirFiles = dirInfo.GetDirectoriesSafe(searchPattern, searchOption);
                        result.AddRange(subdirFiles);
                    }
                    catch (DirectoryNotFoundException)
                    {
                        Log.Warn($"Virtualization error - folder seems to exist, but in fact it does not: {dirInfo.FullName}");
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Warn($"Failed to access subdirectories in directory {directory.FullName}", ex);
            }
        }

        return result.ToArray();
    }
    
    /// <summary>
    /// Recursively deletes a directory along with all its files and subdirectories.
    /// It first resets any special file attributes (like read-only or hidden) to ensure
    /// that all files and directories can be deleted without authorization issues.
    /// </summary>
    /// <param name="directoryInfo">The directory to be deleted.</param>
    public static void RemoveDirectory(this DirectoryInfo directoryInfo)
    {
        foreach (var file in directoryInfo.GetFilesSafe())
        {
            file.Attributes = FileAttributes.Normal;
        }

        foreach (var subDir in directoryInfo.GetDirectoriesSafe())
        {
            RemoveDirectory(subDir); 
        }

        directoryInfo.Delete(true);
    }
}