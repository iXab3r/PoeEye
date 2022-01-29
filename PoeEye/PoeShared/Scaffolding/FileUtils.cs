namespace PoeShared.Scaffolding;

public static class FileUtils
{
    private static readonly IFluentLog Log = typeof(FileUtils).PrepareLogger();

    public static void CopyDirectory(DirectoryInfo sourcePath, DirectoryInfo targetPath)
    {
        CopyDirectory(sourcePath, targetPath, x => true);
    }
    
    public static void CopyDirectory(DirectoryInfo sourceDir, DirectoryInfo targetDir, Predicate<FileInfo> fileFilter)
    {
        Log.Debug(() => $"Copying folder with all content {sourceDir} to {targetDir}");
        Directory.CreateDirectory(targetDir.FullName);

        foreach (var file in sourceDir.GetFiles())
        {
            if (!fileFilter(file))
            {
                Log.Debug(() => $"Skipping file {file.FullName}");
                continue;
            }
            Log.Debug(() => @$"Copying {targetDir.FullName}\{file.Name}");
            file.CopyTo(Path.Combine(targetDir.FullName, file.Name), true);
        }
        
        foreach (var diSourceSubDir in sourceDir.GetDirectories())
        {
            var nextTargetSubDir = targetDir.CreateSubdirectory(diSourceSubDir.Name);
            CopyDirectory(diSourceSubDir, nextTargetSubDir, fileFilter);
        }

        Log.Debug(() => $"Copied folder with all content {sourceDir} to {targetDir}");
    }
}