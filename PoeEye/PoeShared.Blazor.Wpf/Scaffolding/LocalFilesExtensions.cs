using System;
using System.IO;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Wpf.Scaffolding;

public static class LocalFilesExtensions
{
    public static Uri ToLocalFileUri(this FileSystemInfo file)
    {
        if (file == null)
        {
            throw new ArgumentNullException(nameof(file));
        }
        
        // Get the root path (e.g., "C:/")

        var fileUri = new Uri(file.FullName);
        var root = Path.GetPathRoot(fileUri.AbsolutePath) ?? throw new ArgumentException($"Path must be rooted, got {file.FullName}");
        var relativePath = fileUri.AbsolutePath[root.Length..].TrimStart('\\', '/');

        var lowerCaseDrive = file.GetDriveLetter().ToLowerInvariant();

        var httpsUri = $"https://{lowerCaseDrive}/{relativePath}";

        return new Uri(httpsUri);
    }
}