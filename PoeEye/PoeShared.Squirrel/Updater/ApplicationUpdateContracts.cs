#nullable enable
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using PoeShared.Scaffolding;

namespace PoeShared.Squirrel.Updater;

/// <summary>
/// An opaque target selected by a handler. Display labels need not be System.Version values;
/// the same target instance belongs to its selected source and update operation.
/// </summary>
public interface IApplicationUpdateInfo
{
    bool IsUpdateAvailable { get; }
    string InstalledVersionText { get; }
    string TargetVersionText { get; }
    bool IsDowngrade { get; }
    long? DownloadSize { get; }
}

/// <summary>
/// Source-specific operations over a selected target. The application owns operation serialization,
/// source invalidation and cancellation; an accepted local installation outlives the source handler.
/// </summary>
public interface IApplicationUpdateHandler
{
    Task<IApplicationUpdateInfo> CheckAsync(UpdateSourceInfo source, CancellationToken cancellationToken);
    Task<ApplicationUpdateChangelog> GetChangelogAsync(IApplicationUpdateInfo update, CultureInfo culture, CancellationToken cancellationToken);
    Task DownloadAsync(IApplicationUpdateInfo update, IProgressReporter progress, CancellationToken cancellationToken);
    Task ApplyAsync(IApplicationUpdateInfo update, IProgressReporter progress, CancellationToken cancellationToken);
}

/// <summary>
/// Registers a handler for an exact source URI without selecting a source or starting requests.
/// Disposing the token removes that binding; it does not own the application updater or an accepted install.
/// </summary>
public interface IApplicationUpdateHandlerRegistrator
{
    IDisposable Register(Uri uri, IApplicationUpdateHandler handler);
}

/// <summary>Source-owned Markdown and optional link bases for the selected target and requested language.</summary>
public sealed record ApplicationUpdateChangelog(string Markdown, Uri? BaseUri, Uri? DownloadUri);

/// <summary>
/// Application-specific native release-note transport. The target is a real engine version, not an
/// authored revision. Implementations borrow their backend client and await actual I/O before returning.
/// </summary>
public interface INativeApplicationUpdateChangelogProvider
{
    Task<ApplicationUpdateChangelog> GetChangelogAsync(Version targetVersion, CultureInfo culture, CancellationToken cancellationToken);
}

/// <summary>
/// A fixed publication offered by a source. Its expected SHA-256 is verified by the application ZIP service;
/// constructing this descriptor does not establish that downloaded bytes are trusted or prepared.
/// </summary>
public sealed record ZipUpdateArtifact(Uri Url, string Sha256, long? Size);

/// <summary>
/// Application-owned verified ZIP preparation. Callers cannot construct a token or choose/expose its
/// staging path. Prepared bytes belong to this app lifetime; installation and successor readiness are separate.
/// </summary>
public sealed class PreparedZipUpdate
{
    internal PreparedZipUpdate(string sha256, long size, string payloadDirectory, IApplicationZipUpdateService owner)
    {
        Sha256 = sha256;
        Size = size;
        PayloadDirectory = payloadDirectory;
        Owner = owner;
    }

    public string Sha256 { get; }
    public long Size { get; }

    internal string PayloadDirectory { get; }
    internal IApplicationZipUpdateService Owner { get; }
}

/// <summary>
/// Owns ZIP download, verification, staging and local installation. Cancellation applies before plan handoff;
/// an accepted plan is independent of the author handler, its HTTP client and script lifetime.
/// </summary>
public interface IApplicationZipUpdateService
{
    Task<PreparedZipUpdate> PrepareAsync(ZipUpdateArtifact artifact, IProgressReporter progress, CancellationToken cancellationToken);
    Task ApplyAsync(PreparedZipUpdate prepared, IProgressReporter progress, CancellationToken cancellationToken);
}

/// <summary>
/// An updater-cycle progress sink retaining the legacy numeric reporter. A null percent means unknown,
/// while zero is measured progress; reports belong to the active cycle and cannot revive a closed cycle.
/// </summary>
public interface IApplicationUpdateProgressReporter : IProgressReporter
{
    void ReportStage(string stage, double? percent);
}

/// <summary>Result of one public updater operation, distinct from successful installation of a successor.</summary>
public enum UpdateOperationOutcome
{
    Completed,
    Busy,
    Disabled,
    NotConfigured,
    Invalidated,
    /// <summary>The local installation plan was accepted; the original process may exit before returning.</summary>
    ApplyAccepted
}

/// <summary>Observable state of the single application-owned updater and its current selected target.</summary>
public enum ApplicationUpdatePhase
{
    NotConfigured,
    Disabled,
    Idle,
    Checking,
    UpdateAvailable,
    Preparing,
    Downloading,
    ReadyToApply,
    Applying,
    Failed,
    /// <summary>
    /// The local plan has been handed off to the acknowledged product helper. Exactly one synchronous phase notification precedes parent shutdown;
    /// observers may dispose author resources or configure another source without changing the accepted plan.
    /// The selected target remains fixed and incompatible operations remain Busy during that notification.
    /// </summary>
    HandedOff
}
