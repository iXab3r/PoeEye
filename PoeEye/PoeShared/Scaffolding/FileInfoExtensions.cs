namespace PoeShared.Scaffolding;

public static class FileInfoExtensions
{
    /// <summary>
    /// Gets the drive letter from a given <see cref="FileSystemInfo"/> object.
    /// </summary>
    /// <param name="file">The <see cref="FileSystemInfo"/> object to extract the drive letter from.</param>
    /// <returns>The drive letter of the file system object. Returns null if the path is not rooted.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the input file system object is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the file system object does not have a rooted path.</exception>
    public static string GetDriveLetter(this FileSystemInfo file)
    {
        if (file == null)
        {
            throw new ArgumentNullException(nameof(file), "Input file system object cannot be null.");
        }

        var fullPath = file.FullName;
        var driveLetter = Path.GetPathRoot(fullPath)?.TrimEnd('\\', '/', ':');

        return driveLetter;
    }

    /// <summary>
    /// Copies the current file to the specified destination using managed streams (no native File.Copy or CopyFile).
    /// Reason why this method exists is due to problem in virtualization - Kernel32.CopyFile does not properly work
    /// </summary>
    /// <param name="source">The source file (must be a FileInfo).</param>
    /// <param name="destinationPath">Full destination file path.</param>
    /// <param name="overwrite">Whether to overwrite the file if it already exists.</param>
    /// <param name="bufferSize">The size of the buffer for the copy operation.</param>
    public static void CopyToUsingStreams(this FileSystemInfo source, string destinationPath, bool overwrite = false, int bufferSize = 81920)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (destinationPath == null) throw new ArgumentNullException(nameof(destinationPath));

        if (source is not FileInfo sourceFile)
        {
            throw new NotSupportedException("CopyToStreamed is only supported for FileInfo objects.");
        }

        var destinationFile = new FileInfo(destinationPath);

        if (!sourceFile.Exists)
        {
            throw new FileNotFoundException("Source file not found", sourceFile.FullName);
        }

        if (destinationFile.Exists && !overwrite)
        {
            throw new IOException($"Destination file already exists: {destinationFile.FullName}");
        }

        destinationFile.Directory?.Create();

        using var sourceStream = new FileStream(sourceFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var destinationStream = new FileStream(destinationFile.FullName, FileMode.Create, FileAccess.Write, FileShare.None);

        sourceStream.CopyTo(destinationStream, bufferSize);
    }
}