using System.IO.Compression;
using System.Text;
using ByteSizeLib;
using JetBrains.Annotations;
using PoeShared.Logging;

namespace PoeShared.Scaffolding;

/// <summary>
/// Provides ZIP extraction helpers for both conventional extraction and deterministic archive mirroring.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Extract"/> behaves like a normal extraction API: it creates the destination when needed,
/// overwrites colliding files, and preserves files that are not represented by archive entries.
/// </para>
/// <para>
/// <see cref="Mirror"/> is intended for staging and CI-like scenarios where the destination must end
/// up matching the archive contents. It extracts into a temporary sibling directory, writes a completion
/// marker, and then replaces the destination with the staged directory.
/// </para>
/// <para>
/// Both operations validate archive entries before touching disk and reject rooted or traversal paths
/// so ZIP contents cannot escape the requested destination directory.
/// </para>
/// </remarks>
internal static class ArchiveMirror
{
    private const string CompletionMarkerFileName = ".mirrored-archive";

    private static readonly IFluentLog Log = typeof(ArchiveMirror).PrepareLogger();

    private sealed record PlanItem(
        string Action,
        string EntryPath,
        string DestinationPath,
        string DestinationState,
        string Size);

    /// <summary>
    /// Extracts a ZIP archive into the destination directory without deleting unrelated files.
    /// </summary>
    /// <param name="archive">The ZIP file to extract.</param>
    /// <param name="destination">The directory that receives the extracted content.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="archive"/> or <paramref name="destination"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="FileNotFoundException">Thrown when the archive file does not exist.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the destination does not resolve to a usable directory path.
    /// </exception>
    /// <exception cref="InvalidDataException">
    /// Thrown when the archive contains a rooted path or a path that would escape the destination directory.
    /// </exception>
    /// <remarks>
    /// Existing files that collide with archive entries are overwritten. Files and folders that are not
    /// part of the archive are intentionally preserved.
    /// </remarks>
    public static void Extract(FileInfo archive, DirectoryInfo destination)
    {
        ValidateInputs(archive, destination);
        destination.Create();

        using var zipArchive = ZipFile.OpenRead(archive.FullName);
        var entries = GetArchiveEntries(zipArchive);
        Log.Info(BuildPlanMessage("Extract", archive, destination, BuildExtractPlan(destination, entries)));
        ExtractEntries(destination, entries);
    }

    /// <summary>
    /// Mirrors a ZIP archive into the destination directory so the final directory contents match the archive.
    /// </summary>
    /// <param name="archive">The ZIP file whose contents should become the destination state.</param>
    /// <param name="destination">The directory that should be replaced by the mirrored archive content.</param>
    /// <returns><see langword="true"/> when the staged content is committed successfully.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="archive"/> or <paramref name="destination"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="FileNotFoundException">Thrown when the archive file does not exist.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the destination does not resolve to a usable directory path.
    /// </exception>
    /// <exception cref="InvalidDataException">
    /// Thrown when the archive contains a rooted path or a path that would escape the destination directory.
    /// </exception>
    /// <remarks>
    /// The mirror operation is optimized for deterministic final state rather than conservative in-place updates.
    /// Any pre-existing files that are not part of the archive are removed as part of the replacement.
    /// </remarks>
    public static bool Mirror(FileInfo archive, DirectoryInfo destination)
    {
        ValidateInputs(archive, destination);
        destination.Parent?.Create();

        var stagingDirectory = CreateWorkingDirectory(destination, "staging");
        var stagingCommitted = false;

        try
        {
            IReadOnlyList<ZipArchiveEntry> entries;
            using (var zipArchive = ZipFile.OpenRead(archive.FullName))
            {
                entries = GetArchiveEntries(zipArchive);
                Log.Info(BuildPlanMessage("Mirror", archive, destination, BuildMirrorPlan(destination, entries)));

                stagingDirectory.Create();
                ExtractEntries(stagingDirectory, entries);
                WriteCompletionMarker(stagingDirectory, archive);
            }

            if (destination.Exists)
            {
                destination.Delete(true);
            }

            stagingDirectory.MoveTo(destination.FullName);
            stagingCommitted = true;
            return true;
        }
        catch
        {
            throw;
        }
        finally
        {
            if (!stagingCommitted)
            {
                TryDeleteDirectory(stagingDirectory);
            }
        }
    }

    /// <summary>
    /// Enumerates archive entries after validating that each entry is safe to materialize under the destination.
    /// </summary>
    /// <param name="zipArchive">The opened ZIP archive.</param>
    /// <returns>A validated list of archive entries.</returns>
    private static IReadOnlyList<ZipArchiveEntry> GetArchiveEntries(ZipArchive zipArchive)
    {
        var entries = new List<ZipArchiveEntry>();
        foreach (var entry in zipArchive.Entries)
        {
            ValidateArchiveEntry(entry.FullName);
            entries.Add(entry);
        }

        return entries;
    }

    /// <summary>
    /// Builds a logging plan for plain extraction by mapping each archive entry to the current destination state.
    /// </summary>
    /// <param name="destination">The destination directory that will receive the extracted entries.</param>
    /// <param name="entries">The validated archive entries.</param>
    /// <returns>A list of plan rows used to produce structured extraction logs.</returns>
    private static IReadOnlyList<PlanItem> BuildExtractPlan(DirectoryInfo destination, IReadOnlyList<ZipArchiveEntry> entries)
    {
        var rows = new List<PlanItem>();
        foreach (var entry in entries)
        {
            var destinationPath = Path.GetFullPath(Path.Combine(destination.FullName, entry.FullName));
            var destinationState = File.Exists(destinationPath)
                ? "File"
                : Directory.Exists(destinationPath)
                    ? "Directory"
                    : "Missing";

            rows.Add(new PlanItem(
                Action: entry.FullName.EndsWith("/", StringComparison.Ordinal) || entry.FullName.EndsWith("\\", StringComparison.Ordinal)
                    ? "Create dir"
                    : destinationState == "Missing"
                        ? "Extract"
                        : "Overwrite",
                EntryPath: entry.FullName,
                DestinationPath: destinationPath,
                DestinationState: destinationState,
                Size: GetEntrySize(entry)));
        }

        return rows;
    }

    /// <summary>
    /// Builds a logging plan for archive mirroring, including stale destination entries that will disappear.
    /// </summary>
    /// <param name="destination">The destination directory that will be replaced.</param>
    /// <param name="entries">The validated archive entries.</param>
    /// <returns>A list of plan rows used to produce structured mirror logs.</returns>
    private static IReadOnlyList<PlanItem> BuildMirrorPlan(DirectoryInfo destination, IReadOnlyList<ZipArchiveEntry> entries)
    {
        var rows = BuildExtractPlan(destination, entries).ToList();
        var destinationEntries = destination.Exists
            ? destination.GetFileSystemInfos("*", SearchOption.AllDirectories)
            : Array.Empty<FileSystemInfo>();

        foreach (var entry in destinationEntries)
        {
            if (IsCompletionMarker(entry))
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(destination.FullName, entry.FullName);
            rows.Add(new PlanItem(
                Action: "Delete stale",
                EntryPath: relativePath,
                DestinationPath: entry.FullName,
                DestinationState: entry is FileInfo file ? $"File ({ByteSize.FromBytes(file.Length)})" : "Directory",
                Size: entry is FileInfo staleFile ? ByteSize.FromBytes(staleFile.Length).ToString() : "-"));
        }

        return rows;
    }

    /// <summary>
    /// Formats a readable multiline log message that summarizes the pending archive operation.
    /// </summary>
    /// <param name="operationName">The operation name shown in the log, such as Extract or Mirror.</param>
    /// <param name="archive">The archive being processed.</param>
    /// <param name="destination">The destination directory for the operation.</param>
    /// <param name="items">The plan rows that describe the work to be performed.</param>
    /// <returns>A formatted log message containing a fixed-width table.</returns>
    private static string BuildPlanMessage(
        string operationName,
        FileInfo archive,
        DirectoryInfo destination,
        IReadOnlyList<PlanItem> items)
    {
        return $"Archive {operationName} plan for {archive.FullName} → {destination.FullName}{Environment.NewLine}" +
               BuildTable(
                   ["Action", "Entry", "Destination", "State", "Size"],
                   items.Select(x => new[] { x.Action, x.EntryPath, x.DestinationPath, x.DestinationState, x.Size }));
    }

    /// <summary>
    /// Materializes the validated archive entries beneath the destination directory.
    /// </summary>
    /// <param name="destination">The root directory that receives extracted files and folders.</param>
    /// <param name="entries">The archive entries to extract.</param>
    /// <remarks>
    /// File entries are written with overwrite semantics. Parent folders are created automatically as needed.
    /// </remarks>
    private static void ExtractEntries(DirectoryInfo destination, IReadOnlyList<ZipArchiveEntry> entries)
    {
        foreach (var entry in entries)
        {
            var targetPath = Path.GetFullPath(Path.Combine(destination.FullName, entry.FullName));
            if (IsDirectoryEntry(entry.FullName))
            {
                Directory.CreateDirectory(targetPath);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? destination.FullName);
            using var sourceStream = entry.Open();
            using var targetStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
            sourceStream.CopyTo(targetStream);
        }
    }

    /// <summary>
    /// Determines whether the archive entry represents an explicit directory placeholder.
    /// </summary>
    /// <param name="entryPath">The raw path stored in the archive.</param>
    /// <returns>
    /// <see langword="true"/> when the entry ends with a directory separator; otherwise, <see langword="false"/>.
    /// </returns>
    private static bool IsDirectoryEntry(string entryPath)
    {
        return entryPath.EndsWith("/", StringComparison.Ordinal) || entryPath.EndsWith("\\", StringComparison.Ordinal);
    }

    /// <summary>
    /// Returns a human-readable size string for an archive entry.
    /// </summary>
    /// <param name="entry">The archive entry being described.</param>
    /// <returns>The formatted file size, or <c>-</c> for directory entries and zero-length placeholders.</returns>
    private static string GetEntrySize(ZipArchiveEntry entry)
    {
        if (IsDirectoryEntry(entry.FullName))
        {
            return "-";
        }

        return entry.Length > 0 ? ByteSize.FromBytes(entry.Length).ToString() : "-";
    }

    /// <summary>
    /// Creates a unique temporary sibling directory used for staging work before the destination is modified.
    /// </summary>
    /// <param name="destination">The final destination directory.</param>
    /// <param name="suffix">A short suffix describing the staging purpose.</param>
    /// <returns>A directory info pointing to a clean working directory path.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the destination does not have a parent directory that can host staging work.
    /// </exception>
    private static DirectoryInfo CreateWorkingDirectory(DirectoryInfo destination, string suffix)
    {
        var parentDirectory = destination.Parent ?? throw new InvalidOperationException($"Destination '{destination.FullName}' does not have a parent directory.");
        var workingDirectory = new DirectoryInfo(Path.Combine(parentDirectory.FullName, $".{destination.Name}.{Guid.NewGuid():N}.{suffix}"));
        if (workingDirectory.Exists)
        {
            workingDirectory.Delete(true);
        }

        return workingDirectory;
    }

    /// <summary>
    /// Validates common archive operation inputs before any filesystem changes are attempted.
    /// </summary>
    /// <param name="archive">The archive file to inspect or extract.</param>
    /// <param name="destination">The target directory for extraction or mirroring.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="archive"/> or <paramref name="destination"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="FileNotFoundException">Thrown when the archive file does not exist.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the destination does not resolve to a usable directory path.
    /// </exception>
    private static void ValidateInputs([NotNull] FileInfo archive, [NotNull] DirectoryInfo destination)
    {
        if (archive == null)
        {
            throw new ArgumentNullException(nameof(archive));
        }

        if (destination == null)
        {
            throw new ArgumentNullException(nameof(destination));
        }

        if (!archive.Exists)
        {
            throw new FileNotFoundException("Archive file was not found.", archive.FullName);
        }

        if (string.IsNullOrWhiteSpace(destination.FullName))
        {
            throw new ArgumentException("Destination directory must be provided.", nameof(destination));
        }
    }

    /// <summary>
    /// Verifies that a ZIP entry path is safe to extract beneath the destination root.
    /// </summary>
    /// <param name="entryPath">The entry path stored in the ZIP archive.</param>
    /// <exception cref="InvalidDataException">
    /// Thrown when the entry is rooted or contains traversal segments that would escape the destination.
    /// </exception>
    /// <remarks>
    /// Empty or whitespace-only entry names are ignored because they do not represent a meaningful filesystem path.
    /// </remarks>
    private static void ValidateArchiveEntry(string entryPath)
    {
        if (string.IsNullOrWhiteSpace(entryPath))
        {
            return;
        }

        if (Path.IsPathRooted(entryPath))
        {
            throw new InvalidDataException($"ZIP entry '{entryPath}' is rooted and is not allowed.");
        }

        var normalized = entryPath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        var segments = normalized.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            if (segment == "..")
            {
                throw new InvalidDataException($"ZIP entry '{entryPath}' would escape the destination directory.");
            }
        }
    }

    /// <summary>
    /// Determines whether the supplied filesystem entry is the internal completion marker.
    /// </summary>
    /// <param name="entry">The filesystem entry to inspect.</param>
    /// <returns><see langword="true"/> when the entry is the completion marker; otherwise, <see langword="false"/>.</returns>
    private static bool IsCompletionMarker(FileSystemInfo entry)
    {
        return string.Equals(entry.Name, CompletionMarkerFileName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Writes the mirror completion marker into the staged destination directory.
    /// </summary>
    /// <param name="destination">The staged directory that is about to become the final destination.</param>
    /// <param name="archive">The archive that produced the staged content.</param>
    /// <remarks>
    /// The marker currently stores only a simple success sentinel and exists so later tooling can recognize
    /// a directory that was produced by the mirror workflow.
    /// </remarks>
    private static void WriteCompletionMarker(DirectoryInfo destination, FileInfo archive)
    {
        var markerPath = Path.Combine(destination.FullName, CompletionMarkerFileName);
        File.WriteAllText(markerPath, "ok");
        Log.Debug($"Wrote archive mirror marker for {archive.FullName} to {markerPath}");
    }

    /// <summary>
    /// Attempts to delete a temporary directory without masking the original failure that triggered cleanup.
    /// </summary>
    /// <param name="directory">The temporary directory to remove when possible.</param>
    /// <remarks>
    /// Cleanup is best-effort here because callers usually care more about the primary failure than a secondary
    /// inability to remove staging artifacts.
    /// </remarks>
    private static void TryDeleteDirectory(DirectoryInfo? directory)
    {
        try
        {
            if (directory is { Exists: true })
            {
                directory.Delete(true);
            }
        }
        catch
        {
        }
    }

    /// <summary>
    /// Builds a fixed-width text table suitable for structured log output.
    /// </summary>
    /// <param name="headers">The table header values.</param>
    /// <param name="rows">The row values to render beneath the headers.</param>
    /// <returns>A multiline string containing the rendered table.</returns>
    private static string BuildTable(IEnumerable<string> headers, IEnumerable<IEnumerable<string>> rows)
    {
        var headerList = headers.ToList();
        var rowList = rows.Select(row => row.ToList()).ToList();
        var widths = new int[headerList.Count];

        for (var i = 0; i < headerList.Count; i++)
        {
            widths[i] = headerList[i].Length;
        }

        foreach (var row in rowList)
        {
            for (var i = 0; i < row.Count && i < widths.Length; i++)
            {
                widths[i] = Math.Max(widths[i], row[i].Length);
            }
        }

        var builder = new StringBuilder();
        AppendRow(builder, headerList, widths);
        AppendSeparator(builder, widths);
        foreach (var row in rowList)
        {
            AppendRow(builder, row, widths);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Appends a single fixed-width table row to the provided string builder.
    /// </summary>
    /// <param name="builder">The builder receiving the rendered row.</param>
    /// <param name="values">The row values to render.</param>
    /// <param name="widths">The precomputed column widths for the full table.</param>
    private static void AppendRow(StringBuilder builder, IReadOnlyList<string> values, IReadOnlyList<int> widths)
    {
        for (var i = 0; i < widths.Count; i++)
        {
            var value = i < values.Count ? values[i] : string.Empty;
            builder.Append(value.PadRight(widths[i] + 2));
        }

        builder.AppendLine();
    }

    /// <summary>
    /// Appends the separator line that divides the table header from the data rows.
    /// </summary>
    /// <param name="builder">The builder receiving the separator line.</param>
    /// <param name="widths">The precomputed column widths for the full table.</param>
    private static void AppendSeparator(StringBuilder builder, IReadOnlyList<int> widths)
    {
        for (var i = 0; i < widths.Count; i++)
        {
            builder.Append(new string('-', widths[i])).Append("  ");
        }

        builder.AppendLine();
    }
}
