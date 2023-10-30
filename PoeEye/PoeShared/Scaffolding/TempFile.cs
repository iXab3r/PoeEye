using System.IO.Abstractions;

namespace PoeShared.Scaffolding;

/// <summary>
/// Represents a temporary file that can be disposed of.
/// </summary>
public sealed class TempFile : IDisposable
{
    private static readonly IFluentLog Log = typeof(TempFile).PrepareLogger();

    /// <summary>
    /// Initializes a new instance of the <see cref="TempFile"/> class.
    /// Creates a copy of the provided source file in a temporary location.
    /// </summary>
    /// <param name="sourceFile">The source file to be copied to a temporary location.</param>
    public TempFile(FileInfo sourceFile)
    {
        Log.Debug($"Creating temporary copy of a file {sourceFile.FullName}");
        File = FileUtils.CopyFileToTemp(sourceFile);
        Log.Debug($"Temporary copy of a file {sourceFile.FullName} is now at @ {File.FullName}");
    }
    
    /// <summary>
    /// Gets the temporary file information.
    /// </summary>
    public FileInfo File { get; }
    
    /// <summary>
    /// Disposes of the temporary file by attempting to delete it.
    /// </summary>
    public void Dispose()
    {
        try
        {
            File.Delete();
        }
        catch (Exception e)
        {
            Log.Warn($"Failed to remove temp script file @ {File.FullName}", e);
        }
    }
}