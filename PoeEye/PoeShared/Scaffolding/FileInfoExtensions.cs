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
}