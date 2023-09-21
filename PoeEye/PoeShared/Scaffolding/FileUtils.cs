using ByteSizeLib;

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

    /// <summary>
    /// Copies a file from a source path to a destination path.
    /// This method uses low-level file streams instead of File.Copy (which uses Kernel32.CopyFileEx internally) to make it more compatible with file system virtualization methods
    /// which often(at least 2 of them) forget to hook them
    /// </summary>
    /// <param name="sourcePath">The path of the source file to be copied.</param>
    /// <param name="destinationPath">The path to where the file should be copied.</param>
    /// <param name="overwrite">A boolean value indicating whether an existing file at the destination path should be overwritten. If set to false and a file exists, an exception will be thrown.</param>
    /// <param name="bufferSize">Buffer size, most SSDs prefer larger buffer(512KB+)</param>
    /// <exception cref="IOException">Thrown when the destination file already exists and overwrite parameter is set to false, or any other IO error occurs.</exception>
    public static void CopyFile(string sourcePath, string destinationPath, bool overwrite, int bufferSize = 512 * 1024)
    {
        var sourceFile = new FileInfo(sourcePath);
        var destinationFile = new FileInfo(destinationPath);
        if (!overwrite && destinationFile.Exists)
        {
            throw new IOException($"Destination file '{destinationFile.FullName}' already exists.");
        }

        Log.Debug($"Copying file {sourceFile.FullName} (exists: {sourceFile.Exists}{(sourceFile.Exists ? $" {ByteSize.FromBytes(sourceFile.Length)}" : "")}) => {destinationFile.FullName} (exists: {destinationFile.Exists}{(destinationFile.Exists ? $" {ByteSize.FromBytes(destinationFile.Length)}" : "")})");
        using var sourceStream = new FileStream(sourceFile.FullName, FileMode.Open, FileAccess.Read);
        using var destStream = new FileStream(destinationFile.FullName, FileMode.Create, FileAccess.Write);
        var buffer = new byte[bufferSize]; 
        int bytesRead;
        while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
        {
            destStream.Write(buffer, 0, bytesRead);
        }
        destinationFile.Refresh();
        if (!destinationFile.Exists)
        {
            throw new FileNotFoundException($"Failed to copy file {sourceFile.FullName} to {destinationFile.FullName}");
        }
        Log.Debug($"Copied file to {destinationFile.FullName}({ByteSize.FromBytes(destinationFile.Length)})");
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

            var targetFilePath = Path.Combine(targetDir.FullName, file.Name);
            Log.Debug(() => @$"Copying {file.FullName} ({ByteSize.FromBytes(file.Length)}) => {targetFilePath}");
            CopyFile(file.FullName, targetFilePath, overwrite: true);
            var targetFile = new FileInfo(targetFilePath);
            if (!targetFile.Exists)
            {
                throw new InvalidStateException($"Failed to copy file {file.FullName} to {targetFilePath}");
            }

            Log.Debug(() => @$"Copied to {targetFilePath} ({ByteSize.FromBytes(targetFile.Length)})");
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