using JetBrains.Annotations;

namespace PoeShared.Scaffolding;

/// <summary>
/// Represents a temporary file that can be disposed of.
/// </summary>
public sealed class TempFile : IDisposable
{
    private static readonly IFluentLog Log = typeof(TempFile).PrepareLogger();

    /// <summary>
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
    /// Creates a file in temporary location.
    /// </summary>
    public TempFile()
    {
        Log.Debug($"Creating temporary file");
        var tempFilePath = Path.GetTempFileName();
        File = new FileInfo(tempFilePath);
        Log.Debug($"Temporary file is now at @ {File.FullName}");
    }
    
    /// <summary>
    /// Creates a file with given extension(starting with '.') in temporary location.
    /// </summary>
    public TempFile([NotNull] string extension)
    {
        if (extension == null)
        {
            throw new ArgumentNullException(nameof(extension));
        }

        Log.Debug($"Creating temporary file with extension {extension}");
        if (!extension.StartsWith('.'))
        {
            throw new ArgumentException($"Extension must start with '.', got {extension}");
        }
        var tempFilePath = $"{Path.GetTempFileName()}{extension}";
        File = new FileInfo(tempFilePath);
        Log.Debug($"Temporary file with extension {extension} is now at @ {File.FullName}");
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

    public override string ToString()
    {
        var builder = new ToStringBuilder(this);
        builder.AppendParameter(nameof(FileInfo.FullName), File.FullName);
        return builder.ToString();
    }
}