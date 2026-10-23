using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Launcher;

/// <summary>
/// Owns a ready product helper until the application commits its immutable installation plan.
/// Disposing before Commit aborts; after Commit the helper owns parent-exit/install/restart independently.
/// Readiness and commitment do not establish that the installed successor is healthy.
/// </summary>
public sealed class DirectoryUpdateHandoff : IDisposable
{
    private static readonly IFluentLog Log = typeof(DirectoryUpdateHandoff).PrepareLogger();
    private const byte Ready = 1;
    private const byte CommitPlan = 2;
    private const byte ReservePlan = 3;
    private static readonly TimeSpan ProtocolTimeout = TimeSpan.FromSeconds(30);
    private readonly NamedPipeServerStream pipe;
    private readonly Process helper;
    private readonly string helperDirectory;
    private bool committed;
    private bool reserved;
    private bool disposed;

    private DirectoryUpdateHandoff(NamedPipeServerStream pipe, Process helper, string helperDirectory)
    {
        this.pipe = pipe;
        this.helper = helper;
        this.helperDirectory = helperDirectory;
    }

    /// <summary>
    /// Copies the captured target bundle to an owned helper directory and starts its existing early launcher mode.
    /// Keeping execution outside the payload prevents a packed launcher's virtual files from changing its inventory.
    /// Returns only after that actual helper
    /// verifies the captured files, binds the original process handle and acknowledges the private pipe.
    /// Cancellation before return closes the pipe and drains the naturally aborting owned helper.
    /// </summary>
    public static async Task<DirectoryUpdateHandoff> StartAsync(string helperExecutable, DirectoryUpdatePlan plan,
        string handoffDirectory, bool installPayload, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(plan);
        token.ThrowIfCancellationRequested();
        await Task.Run(plan.VerifyPayload, token).ConfigureAwait(false);
        var executable = DirectoryUpdatePlan.GuardPath(helperExecutable);
        if (!string.Equals(executable, DirectoryUpdatePlan.Child(plan.PayloadDirectory, plan.ExecutableName), StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The helper must run from the captured target payload, not the destination application.");
        var handoff = DirectoryUpdatePlan.GuardPath(handoffDirectory);
        if (Directory.Exists(handoff) && Directory.EnumerateFileSystemEntries(handoff).Any())
            throw new IOException("A handoff directory cannot be reused.");
        Directory.CreateDirectory(handoff);
        DirectoryUpdatePlan.GuardPath(handoff);
        var planPath = Path.Combine(handoff, "plan.json");
        var planBytes = plan.Serialize();
        WriteNew(planPath, planBytes);
        var helperRoot = DirectoryUpdatePlan.GuardPath(Path.Combine(handoff, "helper"));
        var planHash = Convert.ToHexString(SHA256.HashData(planBytes));
        var pipeName = "PoeShared-DirectoryUpdate-" + Guid.NewGuid().ToString("N");
        var pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        Process? process = null;
        try
        {
            var copiedExecutable = await Task.Run(() => PrepareHelperSnapshot(plan, helperRoot, token), token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            var start = new ProcessStartInfo(copiedExecutable)
            {
                // The existing early launcher writes relative logs. Keep them beside the plan,
                // outside the immutable payload from which its executable/dependencies are loaded.
                WorkingDirectory = handoff,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            start.ArgumentList.Add("--launcherMethod");
            start.ArgumentList.Add(nameof(LauncherMethod.DirectoryUpdate));
            start.ArgumentList.Add("--directoryPlan");
            start.ArgumentList.Add(planPath);
            start.ArgumentList.Add("--directoryPlanSha256");
            start.ArgumentList.Add(planHash);
            start.ArgumentList.Add("--handoffPipe");
            start.ArgumentList.Add(pipeName);
            if (installPayload) start.ArgumentList.Add("--installPayload");
            process = Process.Start(start) ?? throw new InvalidOperationException("The owned update helper did not start.");
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(ProtocolTimeout);
            await pipe.WaitForConnectionAsync(deadline.Token).ConfigureAwait(false);
            if (await ReadByteAsync(pipe, deadline.Token).ConfigureAwait(false) != Ready || process.HasExited)
                throw new IOException("The actual update helper did not acknowledge its fixed plan.");
            token.ThrowIfCancellationRequested();
            return new DirectoryUpdateHandoff(pipe, process, helperRoot);
        }
        catch (Exception failure)
        {
            Exception? abortFailure = null;
            try { pipe.Dispose(); }
            catch (Exception error) { abortFailure = error; }
            if (process != null)
            {
                try { await DrainAbortedHelperAsync(process, helperRoot).ConfigureAwait(false); }
                catch (Exception error) { abortFailure = abortFailure == null ? error : new AggregateException(abortFailure, error); }
            }
            else TryDeleteHelperSnapshot(helperRoot);
            if (abortFailure != null) throw new AggregateException("Update helper acquisition failed and abort/drain was incomplete.", failure, abortFailure);
            throw;
        }
    }

    /// <summary>Transfers the ready plan once; the application must publish HandedOff before shutting its parent down.</summary>
    public void Commit()
    {
        if (disposed) throw new ObjectDisposedException(nameof(DirectoryUpdateHandoff));
        if (committed) throw new InvalidOperationException("The installation handoff has already been committed.");
        pipe.WriteByte(CommitPlan);
        committed = true;
    }

    /// <summary>
    /// Reserves a ready helper for application-owned native finalization. Only a subsequent Commit
    /// arms installation; disposal or parent failure before Commit aborts the helper without starting B.
    /// </summary>
    public void ReserveInstallation()
    {
        if (disposed) throw new ObjectDisposedException(nameof(DirectoryUpdateHandoff));
        if (committed || reserved) throw new InvalidOperationException("The helper has already been transferred.");
        pipe.WriteByte(ReservePlan);
        reserved = true;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { pipe.Dispose(); }
        finally
        {
            if (committed)
            {
                // The ready helper owns the plan and process lifetime after commitment.
                helper.Dispose();
            }
            else DrainAbortedHelperAsync(helper, helperDirectory).GetAwaiter().GetResult();
        }
    }

    internal static async Task RunHelperAsync(string planPath, string expectedHash, string pipeName, bool installPayload)
    {
        byte[] bytes;
        using (var input = DirectoryUpdatePlan.OpenPayload(planPath))
        using (var memory = new MemoryStream())
        {
            input.CopyTo(memory);
            bytes = memory.ToArray();
        }
        if (!string.Equals(Convert.ToHexString(SHA256.HashData(bytes)), expectedHash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The handoff plan differs from the application's captured bytes.");
        var plan = DirectoryUpdatePlan.Deserialize(bytes);
        using var parent = Process.GetProcessById(plan.ParentProcessId);
        _ = parent.SafeHandle;
        if (parent.HasExited || parent.StartTime.ToUniversalTime() != plan.ParentStartedUtc)
            throw new InvalidOperationException("The original application generation is no longer available for handoff.");
        using var self = Process.GetCurrentProcess();
        var helperRoot = DirectoryUpdatePlan.GuardPath(Path.Combine(Path.GetDirectoryName(planPath)!, "helper"));
        var helperExecutable = DirectoryUpdatePlan.Child(helperRoot, plan.ExecutableName);
        if (!string.Equals(DirectoryUpdatePlan.GuardPath(self.MainModule!.FileName!), helperExecutable, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The update helper is not the captured target launcher.");
        using (var image = DirectoryUpdatePlan.OpenPayload(helperExecutable))
            DirectoryUpdatePlan.VerifyFile(image, plan.Files.Single(x => string.Equals(x.RelativePath, plan.ExecutableName, StringComparison.OrdinalIgnoreCase)));
        plan.VerifyPayload();
        using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        int command;
        using (var deadline = new CancellationTokenSource(ProtocolTimeout))
        {
            await pipe.ConnectAsync(deadline.Token).ConfigureAwait(false);
            if (parent.HasExited) throw new InvalidOperationException("The original exited before helper acknowledgement.");
            // This file is written only by the helper, before the private ready signal.
            WriteNew(Path.Combine(Path.GetDirectoryName(planPath)!, "acknowledged.json"), JsonSerializer.SerializeToUtf8Bytes(new
            {
                HelperProcessId = self.Id,
                HelperStartedUtc = self.StartTime.ToUniversalTime(),
                ParentProcessId = parent.Id,
                ParentStartedUtc = parent.StartTime.ToUniversalTime(),
                plan.ExecutableName,
                plan.InstallationDirectory,
                AcknowledgedUtc = DateTime.UtcNow
            }));
            await pipe.WriteAsync(new[] { Ready }, deadline.Token).ConfigureAwait(false);
            await pipe.FlushAsync(deadline.Token).ConfigureAwait(false);
            command = await ReadByteAsync(pipe, deadline.Token).ConfigureAwait(false);
        }
        // Once reserved, the application owns the actual native finalizer. Neither the author
        // caller's cancellation nor the initial acquisition timeout may truncate that real work.
        if (command == ReservePlan) command = await ReadByteAsync(pipe, CancellationToken.None).ConfigureAwait(false);
        if (command != CommitPlan) return;
        // Commit is the last author-owned dependency. EOF/cancellation after it cannot retarget the plan.
        pipe.Dispose();
        await parent.WaitForExitAsync().ConfigureAwait(false);
        if (installPayload) DirectoryUpdateInstaller.Apply(plan);
        else plan.VerifyPayload();
        var executable = DirectoryUpdatePlan.Child(plan.InstallationDirectory, plan.ExecutableName);
        var start = new ProcessStartInfo(executable, plan.Arguments)
        {
            WorkingDirectory = plan.InstallationDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        using var successor = Process.Start(start) ?? throw new InvalidOperationException("The installed launcher did not start.");
    }

    private static async Task<int> ReadByteAsync(Stream stream, CancellationToken token)
    {
        var buffer = new byte[1];
        return await stream.ReadAsync(buffer, token).ConfigureAwait(false) == 0 ? -1 : buffer[0];
    }

    private static void WriteNew(string path, byte[] bytes)
    {
        path = DirectoryUpdatePlan.GuardPath(path);
        using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        output.Write(bytes);
        output.Flush(flushToDisk: true);
    }

    private static string PrepareHelperSnapshot(DirectoryUpdatePlan plan, string helperRoot, CancellationToken token)
    {
        var payload = Path.TrimEndingDirectorySeparator(plan.PayloadDirectory);
        if (helperRoot.Equals(payload, StringComparison.OrdinalIgnoreCase) ||
            helperRoot.StartsWith(payload + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            payload.StartsWith(helperRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The helper image and immutable payload must have separate roots.");
        if (Directory.Exists(helperRoot)) throw new IOException("A helper snapshot cannot be reused.");
        Directory.CreateDirectory(helperRoot);
        try
        {
            foreach (var file in plan.Files)
            {
                token.ThrowIfCancellationRequested();
                var target = DirectoryUpdatePlan.Child(helperRoot, file.RelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(DirectoryUpdatePlan.Child(plan.PayloadDirectory, file.RelativePath), target);
            }
            // Capture only from the supplying process, outside the new image's virtual filesystem.
            // The running packed helper must verify the original payload, never enumerate its own root.
            var snapshot = DirectoryUpdatePlan.Create(helperRoot, plan.InstallationDirectory, plan.PreviousExecutableName,
                plan.ExecutableName, plan.Arguments, plan.ParentProcessId, plan.ParentStartedUtc);
            if (!snapshot.Files.OrderBy(x => x.RelativePath, StringComparer.Ordinal)
                    .SequenceEqual(plan.Files.OrderBy(x => x.RelativePath, StringComparer.Ordinal)))
                throw new InvalidDataException("The helper snapshot differs from the captured target bundle.");
            token.ThrowIfCancellationRequested();
            return DirectoryUpdatePlan.Child(helperRoot, plan.ExecutableName);
        }
        catch { TryDeleteHelperSnapshot(helperRoot); throw; }
    }

    /// <summary>
    /// Reclaims helper copies in the calling application's handoff directory after their acknowledged
    /// OS generation has exited. Live or unreadable generations are retained; plans and diagnostics stay intact.
    /// </summary>
    public static void CleanupCompletedHelpers(string handoffsDirectory)
    {
        try
        {
            var root = DirectoryUpdatePlan.GuardPath(handoffsDirectory);
            if (!Directory.Exists(root)) return;
            foreach (var directory in Directory.EnumerateDirectories(root))
            {
                if (!Guid.TryParseExact(Path.GetFileName(directory), "N", out _)) continue;
                try
                {
                    DirectoryUpdatePlan.GuardPath(directory);
                    var helperRoot = DirectoryUpdatePlan.GuardPath(Path.Combine(directory, "helper"));
                    var acknowledgement = DirectoryUpdatePlan.Child(directory, "acknowledged.json");
                    if (!Directory.Exists(helperRoot) || !File.Exists(acknowledgement)) continue;
                    using var document = JsonDocument.Parse(File.ReadAllBytes(acknowledgement));
                    var id = document.RootElement.GetProperty("HelperProcessId").GetInt32();
                    var birth = document.RootElement.GetProperty("HelperStartedUtc").GetDateTime();
                    if (birth.Kind != DateTimeKind.Utc) continue;
                    try
                    {
                        using var process = Process.GetProcessById(id);
                        if (!process.HasExited && process.StartTime.ToUniversalTime() == birth) continue;
                    }
                    catch (ArgumentException) { /* The acknowledged PID no longer exists. */ }
                    TryDeleteHelperSnapshot(helperRoot);
                }
                catch (Exception error) { Log.Warn($"Could not reconcile completed update helper in {directory}", error); }
            }
        }
        catch (Exception error) { Log.Warn($"Could not inspect update helpers in {handoffsDirectory}", error); }
    }

    private static void TryDeleteHelperSnapshot(string directory)
    {
        try
        {
            var root = DirectoryUpdatePlan.GuardPath(directory);
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
        catch (Exception error) { Log.Warn($"Could not remove completed update helper copy {directory}", error); }
    }

    private static async Task DrainAbortedHelperAsync(Process helper, string helperDirectory)
    {
        try
        {
            await helper.WaitForExitAsync().WaitAsync(ProtocolTimeout).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            // Never kill a process to turn uncertain handoff into success. Retain its actual handle
            // until exit and report the owned identity so the caller can reconcile its application.
            _ = ReleaseAfterExitAsync(helper, helperDirectory);
            throw new TimeoutException($"Owned uncommitted update helper {helper.Id}/{helper.StartTime.ToUniversalTime():O} has not exited.");
        }
        helper.Dispose();
        TryDeleteHelperSnapshot(helperDirectory);
    }

    private static async Task ReleaseAfterExitAsync(Process helper, string helperDirectory)
    {
        try
        {
            await helper.WaitForExitAsync().ConfigureAwait(false);
            TryDeleteHelperSnapshot(helperDirectory);
        }
        finally { helper.Dispose(); }
    }
}
