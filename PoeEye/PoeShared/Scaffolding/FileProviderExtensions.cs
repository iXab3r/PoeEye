using Microsoft.Extensions.FileProviders;

namespace PoeShared.Scaffolding;

public static class FileProviderExtensions
{
     /// <summary>
    /// Retrieves the file information from the specified file provider or throws an exception if the file is not found.
    /// </summary>
    /// <param name="fileProvider">The file provider to query.</param>
    /// <param name="path">The path to the file within the file provider.</param>
    /// <returns>The file information of the requested file.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the specified file cannot be found.</exception>
    public static IFileInfo GetFileInfoOrThrow(this IFileProvider fileProvider, string path)
    {
        var file = fileProvider.GetFileInfo(path);
        if (file is NotFoundFileInfo)
        {
            throw new FileNotFoundException($"Could not find file @ {path} in file provider", path);
        }

        return file;
    }

    /// <summary>
    /// Reads all bytes from a file in the specified file provider.
    /// </summary>
    /// <param name="fileProvider">The file provider to query.</param>
    /// <param name="path">The path to the file within the file provider.</param>
    /// <returns>A byte array containing the contents of the file.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the specified file cannot be found.</exception>
    /// <exception cref="IOException">Thrown if an I/O error occurs during the read operation.</exception>
    public static byte[] ReadAllBytes(this IFileProvider fileProvider, string path)
    {
        var file = GetFileInfoOrThrow(fileProvider, path);
        using var stream = file.CreateReadStream();
        return stream.ReadToEnd();
    }

    /// <summary>
    /// Reads all text from a file in the specified file provider.
    /// </summary>
    /// <param name="fileProvider">The file provider to query.</param>
    /// <param name="path">The path to the file within the file provider.</param>
    /// <returns>A string containing the text content of the file.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the specified file cannot be found.</exception>
    /// <exception cref="IOException">Thrown if an I/O error occurs during the read operation.</exception>
    public static string ReadAllText(this IFileProvider fileProvider, string path)
    {
        var file = GetFileInfoOrThrow(fileProvider, path);
        using var stream = file.CreateReadStream();
        using var streamReader = new StreamReader(stream);
        return streamReader.ReadToEnd();
    }
}