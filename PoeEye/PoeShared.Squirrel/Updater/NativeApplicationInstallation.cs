using System;
using System.Threading;
using System.Threading.Tasks;
using PoeShared.Scaffolding;
using PoeShared.Squirrel.Core;

namespace PoeShared.Squirrel.Updater;

/// <summary>
/// Application-internal native engine operations over one captured source and its actual release result.
/// Preparation awaits the engine's complete extraction; it neither accepts an installation nor restarts the app.
/// </summary>
internal interface INativeApplicationInstallation
{
    Task<IPoeUpdateInfo> CheckAsync(UpdateSourceInfo source, CancellationToken cancellationToken);
    Task DownloadAsync(IPoeUpdateInfo update, UpdateSourceInfo source, IProgressReporter progress, CancellationToken cancellationToken);
    Task<NativeApplicationInstallation> PrepareAsync(IPoeUpdateInfo update, UpdateSourceInfo source,
        IProgressReporter progress, CancellationToken cancellationToken);
}

/// <summary>
/// The native engine's separate staging snapshot, fixed destination, and application-owned finalizer.
/// The finalizer may publish native installation state only after the ready helper has been reserved;
/// its completion permits final Commit and parent shutdown. Staging alone is not installation acceptance.
/// </summary>
internal sealed record NativeApplicationInstallation(string PayloadDirectory, string ExecutableName,
    string InstallationDirectory, Func<Task> FinalizeAsync);
