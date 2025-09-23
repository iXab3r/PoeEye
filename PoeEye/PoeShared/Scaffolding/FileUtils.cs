using System.Text.RegularExpressions;
using ByteSizeLib;
using JetBrains.Annotations;

namespace PoeShared.Scaffolding;

public static class FileUtils
{
    private static readonly IFluentLog Log = typeof(FileUtils).PrepareLogger();

    /// <summary>
    /// Mirrors source into destination using a simple plan-based copy.
    /// Logs a file-by-file table with hashes and copies only what is needed.
    /// A marker file named ".copied" is stored in the destination to indicate completion.
    /// </summary>
    /// <param name="source">Source directory to mirror from.</param>
    /// <param name="destination">Destination directory to mirror into.</param>
    public static bool MirrorDirectory(DirectoryInfo source, DirectoryInfo destination)
    {
        return DirectoryMirror.Mirror(source, destination);
    }

    public static void RemoveEverythingInside(this DirectoryInfo directory)
    {
        if (!directory.Exists) 
        {
            return;
        }

        foreach (var subDir in directory.GetDirectories())
        {
            subDir.Delete(true);
        }

        foreach (var file in directory.GetFiles())
        {
            file.Delete();
        }
    }

    public static void RemoveDirectoryIfEmpty(this DirectoryInfo directory)
    {
        if (!directory.Exists)
        {
            return;
        }
        var hasFiles = directory.EnumerateFiles().Any();
        var hasDirectories = directory.EnumerateDirectories().Any();
        if (directory.Exists && !hasFiles && !hasDirectories)
        {
            directory.Delete(recursive: true);
        }
    }

    /// <summary>
    /// Creates a temporary copy of the specified file and returns a <see cref="FileStream"/> for it. 
    /// The temporary file will be deleted upon closing the returned <see cref="FileStream"/>.
    /// </summary>
    /// <param name="sourceFile">Source file to be copied.</param>
    /// <returns>A <see cref="FileStream"/> that provides access to the temporary copy of the file. The file is opened in read-write mode with read-write sharing and is set to be deleted upon closing the stream.</returns>
    /// <exception cref="System.IO.IOException">Thrown when the file copy operation fails.</exception>
    /// <remarks>
    /// The method ensures the temporary file has a unique name by appending the original file name to a system-generated temporary file name.
    /// </remarks>
    public static FileInfo CopyFileToTemp(FileInfo sourceFile)
    {
        var fileName = Path.GetFileName(sourceFile.FullName);
        var tempFilePath = Path.GetTempFileName();
        var tempFileName = Path.GetFileNameWithoutExtension(tempFilePath);
        var tempDirectory = Path.GetDirectoryName(tempFilePath) ?? throw new ArgumentException($"Failed to get temp file directory from {tempFilePath}");
        var destinationPath = Path.Combine(tempDirectory, $"{Path.GetFileNameWithoutExtension(fileName)}_{tempFileName}{Path.GetExtension(fileName)}");

        return CopyFile(sourceFile.FullName, destinationPath, overwrite: true);
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
    public static FileInfo CopyFile(string sourcePath, string destinationPath, bool overwrite, int bufferSize = 512 * 1024)
    {
        var sourceFile = new FileInfo(sourcePath);
        var destinationFile = new FileInfo(destinationPath);
        if (!overwrite && destinationFile.Exists)
        {
            throw new IOException($"Destination file '{destinationFile.FullName}' already exists.");
        }

        var sw = Stopwatch.StartNew();
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
        sw.Stop();
        
        if (!destinationFile.Exists)
        {
            throw new FileNotFoundException($"Failed to copy file {sourceFile.FullName} to {destinationFile.FullName}");
        }
        Log.Debug($"Copied file to {destinationFile.FullName}({ByteSize.FromBytes(destinationFile.Length)} in {sw.ElapsedMilliseconds:F0}ms)");
        return destinationFile;
    }

    public static void CopyDirectory(DirectoryInfo sourcePath, DirectoryInfo targetPath)
    {
        CopyDirectory(sourcePath, targetPath, x => true);
    }

    public static void CopyDirectory(DirectoryInfo sourceDir, DirectoryInfo targetDir, Predicate<FileInfo> fileFilter)
    {
        //FIXME This method contains a bunch of virtualization hacks, in some cases files returned by enumeration even though there are no such files/folders
        Log.Debug($"Copying folder with all content {sourceDir} to {targetDir}");
        if (!Directory.Exists(targetDir.FullName))
        {
            Log.Debug($"Creating target directory {targetDir}");
            Directory.CreateDirectory(targetDir.FullName);
        }

        var filesToCopy = sourceDir.GetFiles();
        Log.Debug($"Files to copy: {filesToCopy.Length}\n\t{filesToCopy.Select(x => $"{x.FullName} (exists: {x.Exists})").DumpToTable()}");

        foreach (var fileToCopy in filesToCopy)
        {
            if (!fileFilter(fileToCopy))
            {
                Log.Debug($"Skipping file {fileToCopy.FullName}");
                continue;
            }
            
            if (!fileToCopy.Exists)
            {
                Log.Warn($"Virtualization error - file seems to exist, but in fact it does not: {fileToCopy.FullName}");
                continue;
            }

            var targetFilePath = Path.Combine(targetDir.FullName, fileToCopy.Name);
            Log.Debug(@$"Copying {fileToCopy.FullName} ({ByteSize.FromBytes(fileToCopy.Length)}) => {targetFilePath}");
            CopyFile(fileToCopy.FullName, targetFilePath, overwrite: true);
            var targetFile = new FileInfo(targetFilePath);
            if (!targetFile.Exists)
            {
                throw new InvalidStateException($"Failed to copy file {fileToCopy.FullName} to {targetFilePath}");
            }

            Log.Debug(@$"Copied to {targetFilePath} ({ByteSize.FromBytes(targetFile.Length)})");
        }

        var foldersToCopy = sourceDir.GetDirectories();
        Log.Debug($"Folders to copy: {foldersToCopy.Length}\n\t{foldersToCopy.Select(x => $"{x.FullName} (exists: {x.Exists})").DumpToTable()}");
        foreach (var folderToCopy in foldersToCopy)
        {
            if (!folderToCopy.Exists)
            {
                Log.Warn($"Virtualization error - folder seems to exist, but in fact it does not: {folderToCopy.FullName}");
                continue;
            }
            var targetDirectory = new DirectoryInfo(Path.Combine(targetDir.FullName, folderToCopy.Name));
            try
            {
                CopyDirectory(folderToCopy, targetDirectory, fileFilter);
            }
            catch (DirectoryNotFoundException)
            {
                Log.Warn($"Virtualization error - folder seems to exist, but in fact it does not: {folderToCopy.FullName}");
            }
        }
        Log.Debug($"Copied folder with all content {sourceDir} to {targetDir}");
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
}