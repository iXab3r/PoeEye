using System;
using System.Net;
using System.Net.Http;
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
    /// WHAT: Explicit Download retries the same selected publication after a lost preparation job.
    /// Regression: M07's real backend restart returned HTTP 404 for the old job; the owner cleared
    /// the selected R42 and rejected the user's retry before the handler could prepare it again.
    /// HOW: Drive the real owner through Check, a controlled external-handler 404, and another
    /// Download without Check; require the exact target instance, cleared progress and final readiness.
    /// The existing real HTTP mini-app scenario separately covers preparation, Apply and successor startup.
    /// </summary>
    [Test]
    public async Task ShouldRetrySameSelectedTargetAfterPreparationJobIsLost()
    {
        // Given
        using var config = new TestConfigProvider<UpdateSettingsConfig>();
        using var sources = new UpdateSourceProviderFromConfig(config);
        var uri = new Uri("https://author.example.invalid/pack/");
        sources.ConfigureSources(new[] { new UpdateSourceInfo { Id = "author", Uris = new[] { uri.AbsoluteUri } } }, "author");
        var target = Mock.Of<IApplicationUpdateInfo>(x => x.IsUpdateAvailable &&
            x.InstalledVersionText == "R41" && x.TargetVersionText == "R42");
        var attempts = 0;
        var handler = new Mock<IApplicationUpdateHandler>(MockBehavior.Strict);
        handler.Setup(x => x.CheckAsync(It.IsAny<UpdateSourceInfo>(), It.IsAny<CancellationToken>())).ReturnsAsync(target);
        handler.Setup(x => x.DownloadAsync(target, It.IsAny<IProgressReporter>(), It.IsAny<CancellationToken>()))
            .Returns<IApplicationUpdateInfo, IProgressReporter, CancellationToken>((selected, progress, _) =>
            {
                selected.ShouldBeSameAs(target);
                progress.Update(25);
                return ++attempts == 1
                    ? Task.FromException(new HttpRequestException("Preparation job no longer exists", null, HttpStatusCode.NotFound))
                    : Task.CompletedTask;
            });
        using var owner = new ApplicationUpdateHandlerRegistrator(sources, Mock.Of<IApplicationUpdaterModel>(),
            Mock.Of<IAppArguments>(x => x.Version == new Version(1, 0, 0, 0)),
            Mock.Of<IFactory<INativeApplicationUpdateChangelogProvider>>(), null!);
        using var binding = owner.Register(uri, handler.Object);
        await owner.CheckAsync(CancellationToken.None);

        // When
        var failure = await Should.ThrowAsync<HttpRequestException>(() => owner.DownloadAsync(CancellationToken.None));

        // Then
        failure.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        owner.Phase.ShouldBe(ApplicationUpdatePhase.Failed);
        owner.IsInErrorStatus.ShouldBeTrue();
        owner.IsBusy.ShouldBeFalse();
        owner.CanApplyPreparedUpdate.ShouldBeFalse();
        owner.SelectedUpdate.ShouldBeSameAs(target);
        owner.InstalledVersionText.ShouldBe("R41");
        owner.TargetVersionText.ShouldBe("R42");
        owner.ProgressPercent.ShouldBe(0);
        owner.ProgressStage.ShouldBeEmpty();

        // When
        var retry = await owner.DownloadAsync(CancellationToken.None);

        // Then
        retry.ShouldBe(UpdateOperationOutcome.Completed);
        owner.SelectedUpdate.ShouldBeSameAs(target);
        owner.Phase.ShouldBe(ApplicationUpdatePhase.ReadyToApply);
        owner.CanApplyPreparedUpdate.ShouldBeTrue();
        owner.IsInErrorStatus.ShouldBeFalse();
        owner.IsBusy.ShouldBeFalse();
        handler.Verify(x => x.CheckAsync(It.IsAny<UpdateSourceInfo>(), It.IsAny<CancellationToken>()), Times.Once);
        handler.Verify(x => x.DownloadAsync(target, It.IsAny<IProgressReporter>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        handler.VerifyNoOtherCalls();
    }

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
