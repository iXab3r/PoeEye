using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using PoeShared.Modularity;
using PoeShared.Scaffolding;

namespace PoeShared.Squirrel.Updater;

/// <summary>
/// Downloads and verifies fixed ZIP artifacts into the application's profile-owned staging.
/// Successful preparation and accepted installation outlive author resources.
/// </summary>
internal sealed class ApplicationZipUpdateService : IApplicationZipUpdateService
{
    private readonly string appDataDirectory;
    private readonly ApplicationUpdateInstallationService installation;

    public ApplicationZipUpdateService(IAppArguments appArguments, ApplicationUpdateInstallationService installation)
    {
        appDataDirectory = Path.GetFullPath(appArguments.AppDataDirectory);
        this.installation = installation;
    }

    public Task<PreparedZipUpdate> PrepareAsync(ZipUpdateArtifact artifact, IProgressReporter progress, CancellationToken cancellationToken)
    {
        // The public task owns the complete pipeline, including synchronous ZIP extraction.
        // Keep its filesystem work off a caller's UI dispatcher and await all work before completion.
        return Task.Run(async () =>
        {
            var operationDirectory = GuardOwnedPath(Path.Combine(appDataDirectory, ".updater", "zip", Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(operationDirectory);
            GuardOwnedPath(operationDirectory);
            try
            {
                var archivePath = GuardOwnedPath(Path.Combine(operationDirectory, "artifact.zip"));
                var payloadDirectory = GuardOwnedPath(Path.Combine(operationDirectory, "payload"));
                long size = 0;
                string sha256;
                Report("Downloading", null);
                using (var client = new HttpClient())
                using (var response = await client.GetAsync(artifact.Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    var total = response.Content.Headers.ContentLength ?? artifact.Size;
                    Report("Downloading", total.HasValue ? 0d : null);
                    using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                    await using var input = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                    await using (var output = new FileStream(archivePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 65536, FileOptions.Asynchronous))
                    {
                        var buffer = new byte[65536];
                        int received;
                        while ((received = await input.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false)) != 0)
                        {
                            await output.WriteAsync(buffer.AsMemory(0, received), cancellationToken).ConfigureAwait(false);
                            hash.AppendData(buffer, 0, received);
                            size += received;
                            Report("Downloading", total.HasValue ? 100d * size / total.Value : null);
                        }
                        await output.FlushAsync(cancellationToken).ConfigureAwait(false);
                    }
                    sha256 = Convert.ToHexString(hash.GetHashAndReset());
                }

                cancellationToken.ThrowIfCancellationRequested();
                Report("Verifying", null);
                if (!string.Equals(sha256, artifact.Sha256, StringComparison.OrdinalIgnoreCase) ||
                    (artifact.Size.HasValue && artifact.Size.Value != size))
                    throw new InvalidDataException("The downloaded ZIP does not match the fixed artifact's SHA-256 or size.");

                using (var archive = ZipFile.OpenRead(archivePath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        var destination = Path.GetFullPath(Path.Combine(payloadDirectory, entry.FullName));
                        if (!destination.StartsWith(payloadDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                            throw new InvalidDataException("ZIP payload entries must remain inside the prepared directory.");
                        var relative = Path.GetRelativePath(payloadDirectory, destination);
                        var segments = relative.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var segment in segments)
                            if (segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                                throw new InvalidDataException("ZIP payload entries must use ordinary relative file names.");
                        if (string.Equals(segments[0].TrimEnd(' ', '.'), ".updater", StringComparison.OrdinalIgnoreCase))
                            throw new InvalidDataException("A ZIP payload cannot replace application-owned updater metadata.");
                    }
                }
                cancellationToken.ThrowIfCancellationRequested();
                ZipFile.ExtractToDirectory(archivePath, payloadDirectory);
                cancellationToken.ThrowIfCancellationRequested();
                GuardOwnedPath(payloadDirectory);
                var prepared = new PreparedZipUpdate(sha256, size, payloadDirectory, this);
                Report("Prepared", 100);
                cancellationToken.ThrowIfCancellationRequested();
                return prepared;
            }
            catch (Exception failure)
            {
                // Every operation above is awaited or synchronous. Delete only this operation's
                // positively bounded directory after streams/extraction have actually finished.
                try
                {
                    GuardOwnedPath(operationDirectory);
                    if (Directory.Exists(operationDirectory)) Directory.Delete(operationDirectory, true);
                }
                catch (Exception cleanup)
                {
                    throw new AggregateException("ZIP preparation failed and its owned staging could not be removed.", failure, cleanup);
                }
                throw;
            }

            void Report(string stage, double? percent)
            {
                if (progress is IApplicationUpdateProgressReporter typed) typed.ReportStage(stage, percent);
                else if (percent.HasValue) progress.Update(percent.Value);
            }
        }, cancellationToken);
    }

    public Task ApplyAsync(PreparedZipUpdate prepared, IProgressReporter progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!ReferenceEquals(prepared.Owner, this))
            throw new InvalidOperationException("The prepared ZIP belongs to another application service.");
        var payload = GuardOwnedPath(prepared.PayloadDirectory);
        var executable = Directory.GetFiles(payload, "*.exe", SearchOption.TopDirectoryOnly).Single();
        return installation.ApplyAsync(payload, Path.GetFileName(executable), true, progress, cancellationToken);
    }

    private string GuardOwnedPath(string path)
    {
        var full = Path.GetFullPath(path);
        if (!full.StartsWith(appDataDirectory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("ZIP staging must remain inside the application profile.");
        var current = Path.GetPathRoot(full)!;
        foreach (var segment in full[current.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if ((Directory.Exists(current) || File.Exists(current)) &&
                (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("ZIP staging cannot traverse a reparse point.");
        }
        return full;
    }
}
