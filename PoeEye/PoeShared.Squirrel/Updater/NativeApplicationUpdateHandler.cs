#nullable enable
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Squirrel.Core;

namespace PoeShared.Squirrel.Updater;

/// <summary>Adapts the borrowed native manager's actual tasks and results to the application's existing owner.</summary>
internal sealed class NativeApplicationUpdateHandler : IApplicationUpdateHandler
{
    private readonly IApplicationUpdaterModel model;
    private readonly IAppArguments app;
    private readonly IFactory<INativeApplicationUpdateChangelogProvider> changelogs;
    private readonly ApplicationUpdateInstallationService installations;

    public NativeApplicationUpdateHandler(IApplicationUpdaterModel model, IAppArguments app,
        IFactory<INativeApplicationUpdateChangelogProvider> changelogs, ApplicationUpdateInstallationService installations)
    {
        this.model = model;
        this.app = app;
        this.changelogs = changelogs;
        this.installations = installations;
    }

    public async Task<IApplicationUpdateInfo> CheckAsync(UpdateSourceInfo source, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var capturedSource = source with
        {
            Uris = source.Uris?.ToArray(), Username = source.Username?.Copy(), Password = source.Password?.Copy()
        };
        var result = await NativeInstallation.CheckAsync(capturedSource, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return new NativeUpdateInfo(result, app.Version, capturedSource);
    }

    public Task<ApplicationUpdateChangelog> GetChangelogAsync(IApplicationUpdateInfo update, CultureInfo culture, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return changelogs.Create().GetChangelogAsync(((NativeUpdateInfo)update).TargetVersion, culture, cancellationToken);
    }

    public async Task DownloadAsync(IApplicationUpdateInfo update, IProgressReporter progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (progress is IApplicationUpdateProgressReporter stages) stages.ReportStage("Downloading", null);
        // The native model has no cancellation parameter. Keep its actual task owned until it finishes.
        var native = (NativeUpdateInfo)update;
        await NativeInstallation.DownloadAsync(native.Value, native.Source, progress, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (progress is IApplicationUpdateProgressReporter completed) completed.ReportStage("Prepared", 100);
        else progress.Update(100);
    }

    public async Task ApplyAsync(IApplicationUpdateInfo update, IProgressReporter progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var native = (NativeUpdateInfo)update;
        var prepared = await NativeInstallation.PrepareAsync(native.Value, native.Source, progress, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        await installations.ApplyAsync(prepared.PayloadDirectory, prepared.ExecutableName, true,
            progress, cancellationToken, prepared.InstallationDirectory, prepared.FinalizeAsync).ConfigureAwait(false);
    }

    private INativeApplicationInstallation NativeInstallation => model as INativeApplicationInstallation ??
        throw new InvalidOperationException("The native updater model does not provide application-owned installation preparation.");

    /// <summary>Opaque native target retaining the manager's real release entries, including a no-update result.</summary>
    internal sealed class NativeUpdateInfo : IApplicationUpdateInfo
    {
        public NativeUpdateInfo(IPoeUpdateInfo value, Version installedVersion, UpdateSourceInfo source)
        {
            Value = value;
            Source = source;
            var installed = new Version(installedVersion.Major, installedVersion.Minor,
                Math.Max(0, installedVersion.Build), Math.Max(0, installedVersion.Revision));
            TargetVersion = value.FutureReleaseEntry?.Version?.Version ?? installed;
            IsUpdateAvailable = value.ReleasesToApply.Any();
            InstalledVersionText = installed.ToString();
            TargetVersionText = TargetVersion.ToString();
            IsDowngrade = IsUpdateAvailable && TargetVersion < installed;
            DownloadSize = value.ReleasesToApply.Sum(x => x.Filesize);
        }

        internal IPoeUpdateInfo Value { get; }
        internal UpdateSourceInfo Source { get; }
        internal Version TargetVersion { get; }
        public bool IsUpdateAvailable { get; }
        public string InstalledVersionText { get; }
        public string TargetVersionText { get; }
        public bool IsDowngrade { get; }
        public long? DownloadSize { get; }
    }
}
