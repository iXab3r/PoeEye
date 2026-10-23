using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Squirrel.Core;
using PoeShared.Squirrel.Updater;
using PoeShared.Tests.Helpers;
using Shouldly;
using Squirrel;

namespace PoeShared.Tests.Squirrel.Core;

public sealed class ApplicationUpdateHandlerRegistratorFixture
{
    /// <summary>
    /// WHAT: A generic native Download owns native compatibility dispatch until its actual task drains;
    /// a deliberate legacy operation works afterward with its original Task identity.
    /// Regression: TrackNativeOperation excluded the native binding from Download refusal, admitting a
    /// second manager against the same package/cache root while the first download remained active.
    /// HOW: Drive the real owner Check and Download over a controlled external native-engine seam,
    /// hold that download, observe refusal without invoking the competing delegate, then release/drain
    /// and observe successful compatibility dispatch. Public-entrypoint E2E remains separate coverage.
    /// </summary>
    [Test]
    public async Task ShouldKeepNativeDownloadOwnershipUntilDrainThenAdmitLegacyOperation()
    {
        // Given
        using var config = new TestConfigProvider<UpdateSettingsConfig>();
        using var sources = new UpdateSourceProviderFromConfig(config);
        sources.ConfigureSources(new[] { new UpdateSourceInfo { Id = "native", Name = "Native channel", Uris = new[] { "https://native.example.invalid/" } } }, "native");
        var release = ReleaseEntry.ParseReleaseEntry(new string('A', 40) + " EyeAuras-2.0.0-full.nupkg 1024");
        var update = PoeUpdateInfo.Create(null, Array.Empty<IReleaseEntry>(), new[] { release }, "unused-controlled-native-cache");
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseDownload = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var competingCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var model = new Mock<IApplicationUpdaterModel>();
        var native = model.As<INativeApplicationInstallation>();
        native.Setup(x => x.CheckAsync(It.IsAny<UpdateSourceInfo>(), It.IsAny<CancellationToken>())).ReturnsAsync(update);
        native.Setup(x => x.DownloadAsync(update, It.IsAny<UpdateSourceInfo>(), It.IsAny<IProgressReporter>(), It.IsAny<CancellationToken>()))
            .Callback<IPoeUpdateInfo, UpdateSourceInfo, IProgressReporter, CancellationToken>((_, _, progress, _) =>
            {
                progress.Update(37);
                entered.TrySetResult(true);
            }).Returns(releaseDownload.Task);
        var app = Mock.Of<IAppArguments>(x => x.Version == new Version(1, 0, 0, 0));
        using var owner = new ApplicationUpdateHandlerRegistrator(sources, model.Object, app,
            Mock.Of<IFactory<INativeApplicationUpdateChangelogProvider>>(), null!);
        await owner.CheckAsync(CancellationToken.None);
        var target = owner.SelectedUpdate;
        var started = 0;
        Task StartLegacy() { started++; return competingCompletion.Task; }
        Task Refuse() => Task.FromException(new InvalidOperationException("The updater already has an owner."));
        var download = owner.DownloadAsync(CancellationToken.None);
        Task competing = Task.CompletedTask;
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var progress = owner.ProgressPercent;

            // When
            Action dispatch = () => competing = owner.TrackNativeOperation(StartLegacy, Refuse);
            dispatch.ShouldNotThrow();

            // Then
            started.ShouldBe(0);
            competing.IsFaulted.ShouldBeTrue();
            await Should.ThrowAsync<InvalidOperationException>(async () => await competing);
            download.IsCompleted.ShouldBeFalse();
            owner.IsBusy.ShouldBeTrue();
            owner.SelectedUpdate.ShouldBeSameAs(target);
            owner.ProgressPercent.ShouldBe(progress);
            owner.IsInErrorStatus.ShouldBeFalse();
        }
        finally
        {
            releaseDownload.TrySetResult(true);
            competingCompletion.TrySetResult(true);
            await download.WaitAsync(TimeSpan.FromSeconds(5));
            try { await competing.WaitAsync(TimeSpan.FromSeconds(5)); }
            catch (InvalidOperationException) { /* Expected refusal; the actual task was observed above. */ }
        }

        // When
        var admitted = owner.TrackNativeOperation(StartLegacy, Refuse);
        await admitted;

        // Then
        admitted.ShouldBeSameAs(competingCompletion.Task);
        started.ShouldBe(1);
        owner.IsBusy.ShouldBeFalse();
        owner.SelectedUpdate.ShouldBeSameAs(target);
        owner.Phase.ShouldBe(ApplicationUpdatePhase.ReadyToApply);
        owner.IsInErrorStatus.ShouldBeFalse();
    }
}
