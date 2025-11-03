using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using ByteSizeLib;
using PoeShared.Logging;

namespace PoeShared.Scaffolding;

internal static class DirectoryMirror
{
    private static readonly IFluentLog Log = typeof(DirectoryMirror).PrepareLogger();

    private record PlanItem(
        FileInfo Source,
        FileInfo Destination,
        string RelativePath,
        string Size,
        string CreatedAt,
        string ModifiedAt,
        string SrcHash,
        string DstHash,
        string Action
    );

    /// <summary>
    /// Mirrors source into destination using a simple plan-based copy.
    /// Logs a file-by-file table with hashes and copies only what is needed.
    /// A marker file named ".copied" is stored in the destination to indicate completion.
    /// </summary>
    /// <param name="source">Source directory to mirror from.</param>
    /// <param name="destination">Destination directory to mirror into.</param>
    public static bool Mirror(DirectoryInfo source, DirectoryInfo destination)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (destination == null) throw new ArgumentNullException(nameof(destination));

        destination.Create();

        var markerFile = new FileInfo(Path.Combine(destination.FullName, ".copied"));
        var markerExists = markerFile.Exists;
        var plan = BuildPlan(source, destination, markerExists);

        // Log table
        var table = BuildTable(
            new[] { "Plan", "File", "Size", "Created", "Modified", "SrcHash", "DstHash" },
            plan.Select(x => new[] { x.Action, x.RelativePath, x.Size, x.CreatedAt, x.ModifiedAt, x.SrcHash, x.DstHash })
        );
        Log.Info($"Mirror plan (hash) for {source.FullName} → {destination.FullName} \n" + table);

        // Execute plan
        var copiedAny = ExecutePlan(plan, Log);

        // Update marker with current date
        var stamp = DateTimeOffset.Now.ToString("O");
        WriteMarker(markerFile, stamp, Log);
        return copiedAny;
    }

    private static IReadOnlyList<PlanItem> BuildPlan(DirectoryInfo source, DirectoryInfo destination, bool markerExists)
    {
        var results = new List<PlanItem>();
        var sourceFiles = source.GetFilesSafe("*", SearchOption.AllDirectories);
        foreach (var sourceFile in sourceFiles)
        {
            var relativePath = Path.GetRelativePath(source.FullName, sourceFile.FullName);
            var destinationFile = new FileInfo(Path.Combine(destination.FullName, relativePath));
            var size = ByteSizeLib.ByteSize.FromBytes(sourceFile.Length).ToString();

            // Compute hashes only when marker exists (refresh); for initial copy avoid extra work
            string srcHashStr = markerExists ? ComputeHashHex(sourceFile) : "-";
            string dstHashStr;
            if (!destinationFile.Exists)
            {
                dstHashStr = "(absent)";
            }
            else
            {
                dstHashStr = markerExists ? ComputeHashHex(destinationFile) : "-";
            }

            string action;
            if (!markerExists)
            {
                action = "Copy (initial)";
            }
            else if (!destinationFile.Exists)
            {
                action = "Copy (missing)";
            }
            else
            {
                var isMismatch = !string.Equals(srcHashStr, dstHashStr, StringComparison.OrdinalIgnoreCase)
                                 || sourceFile.Length != destinationFile.Length;
                action = isMismatch ? "Copy (mismatch)" : "Skip";
            }

            results.Add(new PlanItem(sourceFile, destinationFile, relativePath, size,  source.CreationTime.ToString(CultureInfo.InvariantCulture), source.LastWriteTime.ToString(CultureInfo.InvariantCulture), srcHashStr, dstHashStr, action));
        }

        return results;
    }

    private static bool ExecutePlan(IEnumerable<PlanItem> plan, IFluentLog log)
    {
        var copiedAny = false;
        foreach (var item in plan.Where(x => x.Action.StartsWith("Copy", StringComparison.OrdinalIgnoreCase)))
        {
            item.Destination.Directory!.Create();
            log.Debug($"Copying {item.RelativePath}: {item.Source.FullName} → {item.Destination.FullName}");
            item.Source.CopyToUsingStreams(item.Destination.FullName, overwrite: true);
            copiedAny = true;
        }

        return copiedAny;
        
    }

    private static string ComputeHashHex(FileInfo file)
    {
        try
        {
            using var md5 = MD5.Create();
            using var stream = file.OpenRead();
            var hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }
        catch
        {
            return "-";
        }
    }

    private static string BuildTable(IEnumerable<string> headers, IEnumerable<IEnumerable<string>> rows)
    {
        var headerList = headers.ToList();
        var rowList = rows.Select(r => r.ToList()).ToList();

        var colWidths = new int[headerList.Count];
        for (int i = 0; i < headerList.Count; i++)
        {
            colWidths[i] = headerList[i]?.Length ?? 0;
        }
        foreach (var row in rowList)
        {
            for (int i = 0; i < headerList.Count && i < row.Count; i++)
            {
                colWidths[i] = Math.Max(colWidths[i], row[i]?.Length ?? 0);
            }
        }

        var sb = new StringBuilder();

        // Header
        for (int i = 0; i < headerList.Count; i++)
        {
            var h = headerList[i] ?? string.Empty;
            sb.Append(h.PadRight(colWidths[i] + 2));
        }
        sb.AppendLine();
        // Separator
        for (int i = 0; i < headerList.Count; i++)
        {
            sb.Append(new string('-', colWidths[i])).Append("  ");
        }
        sb.AppendLine();

        // Rows
        foreach (var row in rowList)
        {
            for (int i = 0; i < headerList.Count; i++)
            {
                var cell = i < row.Count ? row[i] ?? string.Empty : string.Empty;
                if (i == 0)
                {
                    // Left align file column
                    sb.Append(cell.PadRight(colWidths[i] + 2));
                }
                else
                {
                    // Right align numbers/hashes for readability
                    sb.Append(cell.PadLeft(colWidths[i])).Append("  ");
                }
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static void WriteMarker(FileInfo markerFile, string content, IFluentLog log)
    {
        try
        {
            markerFile.Directory!.Create();
            File.WriteAllText(markerFile.FullName, content);
            log.Debug($"Updated marker @ {markerFile.FullName} with '{content}'");
        }
        catch (Exception e)
        {
            log.Warn($"Failed to update marker @ {markerFile.FullName}: {e.Message}");
        }
    }
}
