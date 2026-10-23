using System;
using System.IO;

namespace PoeShared.Launcher;

/// <summary>
/// Applies a fixed directory payload after the product helper has observed parent exit.
/// File installation is distinct from helper acceptance and launching a ready successor.
/// </summary>
public static class DirectoryUpdateInstaller
{
    public static void Apply(DirectoryUpdatePlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (string.Equals(plan.PayloadDirectory, plan.InstallationDirectory, StringComparison.OrdinalIgnoreCase) ||
            plan.InstallationDirectory.StartsWith(Path.TrimEndingDirectorySeparator(plan.PayloadDirectory) + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Directory installation requires a separate prepared payload.");
        plan.VerifyPayload();
        foreach (var file in plan.Files)
        {
            using var source = DirectoryUpdatePlan.OpenPayload(DirectoryUpdatePlan.Child(plan.PayloadDirectory, file.RelativePath));
            DirectoryUpdatePlan.VerifyFile(source, file);
            source.Position = 0;
            var destination = DirectoryUpdatePlan.Child(plan.InstallationDirectory, file.RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            DirectoryUpdatePlan.GuardPath(destination);
            using var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);
            source.CopyTo(output);
            output.Flush(flushToDisk: true);
        }
        if (!string.Equals(plan.PreviousExecutableName, plan.ExecutableName, StringComparison.OrdinalIgnoreCase))
            File.Delete(DirectoryUpdatePlan.Child(plan.InstallationDirectory, plan.PreviousExecutableName));
    }
}
