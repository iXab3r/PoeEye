namespace PoeShared.Scaffolding;

public static class FileUtils
{
    private static readonly IFluentLog Log = typeof(FileUtils).PrepareLogger();
    
    public static void RemoveFilesInDirectory(this DirectoryInfo directory)
    {
        if (!directory.Exists) {
            return;
        }
        var filesToRemove = directory.GetFiles("*", SearchOption.AllDirectories);
        RemoveFiles(filesToRemove);
    }

    public static void RemoveDirectoryIfEmpty(this DirectoryInfo directory)
    {
        var hasFiles = directory.EnumerateFiles().Any();
        var hasDirectories = directory.EnumerateDirectories().Any();
        if (directory.Exists && !hasFiles && !hasDirectories)
        {
            directory.Delete(recursive: true);
        }
    }

    public static void CopyDirectory(DirectoryInfo sourcePath, DirectoryInfo targetPath)
    {
        CopyDirectory(sourcePath, targetPath, x => true);
    }

    public static void CopyDirectory(DirectoryInfo sourceDir, DirectoryInfo targetDir, Predicate<FileInfo> fileFilter)
    {
        Log.Debug(() => $"Copying folder with all content {sourceDir} to {targetDir}");
        if (!Directory.Exists(targetDir.FullName))
        {
            Log.Debug(() => $"Creating target directory {targetDir}");
            Directory.CreateDirectory(targetDir.FullName);
        }

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

    public static long Seek(Stream stream, byte[] needle)
    {
        var bufferSize = 1024;
        if (bufferSize < needle.Length * 2)
        {
            bufferSize = needle.Length * 2;
        }

        var buffer = new byte[bufferSize];
        var size = bufferSize;
        var offset = 0;
        var position = stream.Position;

        while (true)
        {
            var r = stream.Read(buffer, offset, size);

            // when no bytes are read -- the string could not be found
            if (r <= 0)
            {
                return -1;
            }

            // when less then size bytes are read, we need to slice
            // the buffer to prevent reading of "previous" bytes
            ReadOnlySpan<byte> ro = buffer;
            if (r < size)
            {
                ro = ro.Slice(0, offset + size);
            }

            // check if we can find our search bytes in the buffer
            var i = ro.IndexOf(needle);
            if (i > -1)
            {
                return position + i;
            }

            // when less then size was read, we are done and found nothing
            if (r < size)
            {
                return -1;
            }

            // we still have bytes to read, so copy the last search
            // length to the beginning of the buffer. It might contain
            // a part of the bytes we need to search for

            offset = needle.Length;
            size = bufferSize - offset;
            Array.Copy(buffer, buffer.Length - offset, buffer, 0, offset);
            position += bufferSize - offset;
        }
    }

    private static void RemoveFiles(params FileInfo[] filesToRemove)
    {
        var directoriesToCleanup = new HashSet<DirectoryInfo>();
        foreach (var file in filesToRemove)
        {
            file.Delete();
            if (file.Exists)
            {
                throw new ApplicationException($"Failed to remove file {file}");
            }
            var parentFolder = file.Directory;
            directoriesToCleanup.Add(parentFolder);
        }
	
        foreach (var dir in directoriesToCleanup)
        {
            RemoveDirectoryIfEmpty(dir);
        }
    }
}