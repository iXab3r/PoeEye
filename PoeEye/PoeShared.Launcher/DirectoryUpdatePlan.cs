using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;

namespace PoeShared.Launcher;

/// <summary>
/// Fixed local directory-install inputs captured by the application before helper handoff.
/// Constructing a plan does not establish helper acceptance or successor readiness.
/// </summary>
public sealed class DirectoryUpdatePlan
{
    private DirectoryUpdatePlan(string payloadDirectory, string installationDirectory, string previousExecutableName,
        string executableName, string arguments, int parentProcessId, DateTime parentStartedUtc,
        IEnumerable<DirectoryUpdateFile> files)
    {
        PayloadDirectory = Path.GetFullPath(payloadDirectory);
        InstallationDirectory = Path.GetFullPath(installationDirectory);
        PreviousExecutableName = RequireExecutableName(previousExecutableName);
        ExecutableName = RequireExecutableName(executableName);
        Arguments = arguments ?? string.Empty;
        if (parentProcessId <= 0 || parentStartedUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("An exact parent process identity in UTC is required.");
        ParentProcessId = parentProcessId;
        ParentStartedUtc = parentStartedUtc;
        Files = new ReadOnlyCollection<DirectoryUpdateFile>(files.ToArray());
        if (Files.Count == 0 || Files.Select(x => x.RelativePath).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Files.Count ||
            !Files.Any(x => string.Equals(x.RelativePath, ExecutableName, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("The fixed payload must contain one selected root launcher and unique file paths.");
        foreach (var file in Files)
        {
            RequireRelativePath(file.RelativePath);
            if (file.Length < 0 || file.Sha256.Length != 64 || !file.Sha256.All(Uri.IsHexDigit))
                throw new InvalidDataException("Every payload file needs its captured length and SHA-256.");
        }
        if (!string.Equals(PreviousExecutableName, ExecutableName, StringComparison.OrdinalIgnoreCase) &&
            Files.Any(x => string.Equals(x.RelativePath, PreviousExecutableName, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("A renamed payload cannot also deliver the previous launcher.");
    }

    public string PayloadDirectory { get; }
    public string InstallationDirectory { get; }
    public string PreviousExecutableName { get; }
    public string ExecutableName { get; }
    public string Arguments { get; }
    public int ParentProcessId { get; }
    public DateTime ParentStartedUtc { get; }
    public IReadOnlyList<DirectoryUpdateFile> Files { get; }

    /// <summary>Captures verified payload bytes without modifying the installation or accepting a handoff.</summary>
    public static DirectoryUpdatePlan Create(string payloadDirectory, string installationDirectory,
        string previousExecutableName, string executableName, string arguments, int parentProcessId,
        DateTime parentStartedUtc)
    {
        var payload = GuardPath(payloadDirectory);
        var installation = GuardPath(installationDirectory);
        var files = EnumeratePayload(payload).Select(path =>
        {
            using var input = OpenPayload(path);
            return new DirectoryUpdateFile(Path.GetRelativePath(payload, path).Replace('\\', '/'),
                input.Length, Convert.ToHexString(SHA256.HashData(input)));
        }).ToArray();
        return new DirectoryUpdatePlan(payload, installation, previousExecutableName, executableName, arguments,
            parentProcessId, parentStartedUtc, files);
    }

    internal byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(this);

    internal static DirectoryUpdatePlan Deserialize(byte[] bytes)
    {
        using var document = JsonDocument.Parse(bytes);
        var root = document.RootElement;
        return new DirectoryUpdatePlan(root.GetProperty(nameof(PayloadDirectory)).GetString()!,
            root.GetProperty(nameof(InstallationDirectory)).GetString()!, root.GetProperty(nameof(PreviousExecutableName)).GetString()!,
            root.GetProperty(nameof(ExecutableName)).GetString()!, root.GetProperty(nameof(Arguments)).GetString()!,
            root.GetProperty(nameof(ParentProcessId)).GetInt32(), root.GetProperty(nameof(ParentStartedUtc)).GetDateTime(),
            root.GetProperty(nameof(Files)).EnumerateArray().Select(x => new DirectoryUpdateFile(
                x.GetProperty(nameof(DirectoryUpdateFile.RelativePath)).GetString()!,
                x.GetProperty(nameof(DirectoryUpdateFile.Length)).GetInt64(), x.GetProperty(nameof(DirectoryUpdateFile.Sha256)).GetString()!)));
    }

    internal void VerifyPayload()
    {
        GuardPath(InstallationDirectory);
        var actual = EnumeratePayload(PayloadDirectory).Select(x => Path.GetRelativePath(PayloadDirectory, x).Replace('\\', '/'))
            .OrderBy(x => x, StringComparer.Ordinal).ToArray();
        if (!actual.SequenceEqual(Files.Select(x => x.RelativePath).OrderBy(x => x, StringComparer.Ordinal)))
            throw new InvalidDataException("The prepared payload file set changed after plan capture.");
        foreach (var file in Files)
        {
            using var input = OpenPayload(Child(PayloadDirectory, file.RelativePath));
            VerifyFile(input, file);
        }
    }

    internal static FileStream OpenPayload(string path) => new(GuardPath(path), FileMode.Open, FileAccess.Read, FileShare.Read);

    internal static void VerifyFile(Stream input, DirectoryUpdateFile expected)
    {
        if (input.Length != expected.Length ||
            !string.Equals(Convert.ToHexString(SHA256.HashData(input)), expected.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Prepared bytes changed: {expected.RelativePath}");
    }

    internal static string Child(string root, string relative)
    {
        RequireRelativePath(relative);
        var path = Path.GetFullPath(Path.Combine(root, relative));
        if (!path.StartsWith(Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("An installation file must remain beneath its captured root.");
        return GuardPath(path);
    }

    internal static string GuardPath(string path)
    {
        var full = Path.GetFullPath(path);
        var current = Path.GetPathRoot(full)!;
        foreach (var segment in full[current.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            try
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException($"Directory installation cannot traverse a reparse point: {current}");
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
        }
        return full;
    }

    private static IEnumerable<string> EnumeratePayload(string directory)
    {
        foreach (var path in Directory.EnumerateFileSystemEntries(GuardPath(directory)).OrderBy(x => x, StringComparer.Ordinal))
        {
            GuardPath(path);
            if ((File.GetAttributes(path) & FileAttributes.Directory) != 0)
            {
                foreach (var nested in EnumeratePayload(path)) yield return nested;
            }
            else yield return path;
        }
    }

    private static void RequireRelativePath(string relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative))
            throw new InvalidDataException("Payload paths must be ordinary relative file names.");
        foreach (var segment in relative.Replace('\\', '/').Split('/'))
            if (string.IsNullOrWhiteSpace(segment) || segment is "." or ".." || segment.EndsWith(' ') || segment.EndsWith('.') ||
                segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || string.Equals(segment, ".updater", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Payload paths cannot escape their root or replace updater metadata.");
    }

    private static string RequireExecutableName(string name)
    {
        RequireRelativePath(name);
        if (Path.GetFileName(name) != name || name.Contains('/') || !name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The launcher must be a root executable file name.");
        return name;
    }
}

/// <summary>Immutable relative path and byte identity captured from one verified payload file.</summary>
public sealed record DirectoryUpdateFile(string RelativePath, long Length, string Sha256);
