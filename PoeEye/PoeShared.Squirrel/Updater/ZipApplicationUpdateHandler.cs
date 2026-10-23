using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using PoeShared.Scaffolding;

namespace PoeShared.Squirrel.Updater;

/// <summary>
/// Author protocol boundary for ZIP updates. Derived handlers supply source checks, notes and a fixed artifact;
/// the application ZIP service owns verified preparation independently of author resource lifetime.
/// The latest successful preparation belongs to its exact target instance; Apply forwards that token
/// to the supplied service, which owns installation availability and lifetime.
/// </summary>
public abstract class ZipApplicationUpdateHandler : IApplicationUpdateHandler
{
    private readonly IApplicationZipUpdateService zipUpdates;
    private (IApplicationUpdateInfo Target, PreparedZipUpdate Token)? preparedUpdate;

    protected ZipApplicationUpdateHandler(IApplicationZipUpdateService zipUpdates)
    {
        this.zipUpdates = zipUpdates;
    }

    protected abstract Task<IApplicationUpdateInfo> CheckSourceAsync(UpdateSourceInfo source, CancellationToken cancellationToken);

    protected abstract Task<ApplicationUpdateChangelog> ReadChangelogAsync(IApplicationUpdateInfo update, CultureInfo culture, CancellationToken cancellationToken);

    protected abstract Task<ZipUpdateArtifact> ResolveArtifactAsync(IApplicationUpdateInfo update, IProgressReporter progress, CancellationToken cancellationToken);

    public Task<IApplicationUpdateInfo> CheckAsync(UpdateSourceInfo source, CancellationToken cancellationToken)
    {
        return CheckSourceAsync(source, cancellationToken);
    }

    public Task<ApplicationUpdateChangelog> GetChangelogAsync(IApplicationUpdateInfo update, CultureInfo culture, CancellationToken cancellationToken)
    {
        return ReadChangelogAsync(update, culture, cancellationToken);
    }

    public async Task DownloadAsync(IApplicationUpdateInfo update, IProgressReporter progress, CancellationToken cancellationToken)
    {
        preparedUpdate = null;
        var artifact = await ResolveArtifactAsync(update, progress, cancellationToken).ConfigureAwait(false);
        var prepared = await zipUpdates.PrepareAsync(artifact, progress, cancellationToken).ConfigureAwait(false);
        preparedUpdate = (update, prepared);
    }

    public Task ApplyAsync(IApplicationUpdateInfo update, IProgressReporter progress, CancellationToken cancellationToken)
    {
        var prepared = preparedUpdate;
        if (!prepared.HasValue || !ReferenceEquals(prepared.Value.Target, update))
        {
            throw new InvalidOperationException("Prepare this exact update target before applying it.");
        }
        return zipUpdates.ApplyAsync(prepared.Value.Token, progress, cancellationToken);
    }
}
