using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Moq;
using NUnit.Framework;
using PoeShared.Dialogs.Services;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Services;
using PoeShared.Squirrel.Core;
using PoeShared.Squirrel.Updater;
using PoeShared.Tests.Helpers;
using PoeShared.UI;
using Shouldly;
using Squirrel;
using DispatcherScheduler = PoeShared.Modularity.DispatcherScheduler;

namespace PoeShared.Tests.Squirrel.Core;

[Category("Integration")]
public sealed class ApplicationUpdaterViewModelFixture
{
    /// <summary>
    /// WHAT: Selecting an earlier native release after a manual Check changes both updater presentations
    /// and the public notes target to A, retaining the running B as the installed version.
    /// HOW: Use the real dispatcher, source provider, owner and both updater view models; control only
    /// the external native engine and notes transport. The full A06 scenarios separately prove real UI/install.
    /// </summary>
    [Test]
    public Task ShouldSelectEarlierNativeReleaseForCommonPresentationAndNotes() => RunAsync(async context =>
    {
        // Given
        await context.CheckAsync();
        Version.Parse(context.Updater.TargetVersionText).ShouldBe(new Version(3, 0, 0, 0));

        // When
        await context.SelectEarlierAsync();

        // Then
        context.Updater.LatestUpdate.ShouldBeSameAs(context.Forced);
        context.Updater.LatestVersion.ShouldBe(context.Earlier.Version.Version);
        context.Updater.SelectedUpdate.ShouldNotBeNull();
        Version.Parse(context.Updater.SelectedUpdate.TargetVersionText).ShouldBe(context.Earlier.Version.Version);
        Version.Parse(context.Updater.TargetVersionText).ShouldBe(context.Earlier.Version.Version);
        context.Updater.InstalledVersionText.ShouldBe("2.0.0.0");
        context.Updater.SelectedUpdate.IsDowngrade.ShouldBeTrue();
        context.Updater.IsOpen.ShouldBeTrue("The normal manual native Check opens the existing notes presentation");
        var notes = await context.Updater.GetChangelogAsync(CultureInfo.GetCultureInfo("en"), CancellationToken.None);
        notes.Markdown.ShouldBe("Selected native " + context.Earlier.Version.Version);
        context.Notes.Verify(x => x.GetChangelogAsync(context.Earlier.Version.Version,
            It.Is<CultureInfo>(culture => culture.Name == "en"), It.IsAny<CancellationToken>()), Times.Once);
    });

    /// <summary>
    /// WHAT: A cold B installation asks before installing an earlier A; No preserves A without file work,
    /// and a second attempt asks again before Yes installs that same selected result.
    /// HOW: Drive the public manual Check, native release selection and DownloadUpdate(true) on a real
    /// dispatcher with actual owner/VM state, controlling the external confirmation and native engine only.
    /// No previous Apply populates LatestAppliedVersion. Full A06 separately proves the causal successor.
    /// </summary>
    [Test]
    public Task ShouldDeclineThenConfirmEarlierNativeReleaseFromColdInstallation() => RunAsync(async context =>
    {
        // Given
        await context.CheckAsync();
        await context.SelectEarlierAsync();
        context.Updater.LatestAppliedVersion.ShouldBeNull();

        // When
        await context.Updater.DownloadUpdate(true);

        // Then
        context.Prompts.Count.ShouldBe(1);
        context.Prompts[0].Title.ShouldBe("Version downgrade");
        context.Prompts[0].Content.ShouldContain("v1.0.0");
        context.Prompts[0].Content.ShouldContain("newer 2.0.0");
        context.Updater.LatestUpdate.ShouldBeSameAs(context.Forced);
        context.Updater.LatestAppliedVersion.ShouldBeNull();
        context.Model.Verify(x => x.VerifyRelease(It.IsAny<IPoeUpdateInfo>()), Times.Never);
        context.Model.Verify(x => x.DownloadRelease(It.IsAny<IPoeUpdateInfo>()), Times.Never);
        context.Model.Verify(x => x.ApplyRelease(It.IsAny<IPoeUpdateInfo>()), Times.Never);

        // When
        await context.Updater.DownloadUpdate(true);

        // Then
        context.Prompts.Count.ShouldBe(2);
        context.Prompts[1].ShouldBe(context.Prompts[0]);
        context.Model.Verify(x => x.VerifyRelease(context.Forced), Times.Once);
        context.Model.Verify(x => x.DownloadRelease(context.Forced), Times.Once);
        context.Model.Verify(x => x.ApplyRelease(context.Forced), Times.Once);
        context.Updater.IsInErrorStatus.ShouldBeFalse();
    });

    private static async Task RunAsync(Func<ColdNativeSelection, Task> scenario)
    {
        var dispatcher = SchedulerProvider.Instance.CreateDispatcherScheduler(
            nameof(ApplicationUpdaterViewModelFixture) + Guid.NewGuid().ToString("N"), ThreadPriority.Normal);
        try
        {
            await dispatcher.RunAsync(async () =>
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher.Dispatcher));
                using var context = new ColdNativeSelection(dispatcher.Dispatcher);
                await scenario(context);
            }).Unwrap();
        }
        finally
        {
            await dispatcher.Dispatcher.InvokeAsync(() => dispatcher.Dispatcher.InvokeShutdown());
            dispatcher.Dispatcher.Thread.Join(TimeSpan.FromSeconds(10)).ShouldBeTrue("Owned updater dispatcher must exit");
        }
    }

    private sealed class ColdNativeSelection : IDisposable
    {
        private readonly TestConfigProvider<UpdateSettingsConfig> config = new();
        private readonly UpdateSourceProviderFromConfig sources;
        private readonly ApplicationUpdateHandlerRegistrator owner;
        private readonly Dispatcher dispatcher;

        public ColdNativeSelection(Dispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            config.Save(new UpdateSettingsConfig { AutoUpdateTimeout = TimeSpan.Zero, AutomaticallyDownloadUpdates = false });
            sources = new UpdateSourceProviderFromConfig(config);
            sources.ConfigureSources(new[] { new UpdateSourceInfo
            {
                Id = "Alpha", Name = "Alpha", Uris = new[] { "https://native.example.invalid/" }
            } }, "Alpha");
            Earlier = Release("1.0.0", 'A');
            var installed = Release("2.0.0", 'B');
            var later = Release("3.0.0", 'C');
            var check = PoeUpdateInfo.Create(installed, new[] { installed }, new[] { Earlier, installed, later }, "controlled-native-cache");
            // Native PrepareUpdate receives an empty local list for a forced release, even on installed B.
            Forced = PoeUpdateInfo.Create(null, Array.Empty<IReleaseEntry>(), new[] { Earlier }, "controlled-native-cache");
            var forcedLatest = PoeUpdateInfo.Create(null, Array.Empty<IReleaseEntry>(), new[] { later }, "controlled-native-cache");
            IPoeUpdateInfo latest = null;
            Model.SetupGet(x => x.UpdateSource).Returns(() => sources.UpdateSource);
            Model.SetupGet(x => x.LatestUpdate).Returns(() => latest);
            Model.Setup(x => x.Reset()).Callback(() => latest = null);
            Model.As<INativeApplicationInstallation>()
                .Setup(x => x.CheckAsync(It.IsAny<UpdateSourceInfo>(), It.IsAny<CancellationToken>()))
                .Returns(() => { latest = check; return Task.FromResult(check); });
            Model.Setup(x => x.PrepareForceUpdate(Earlier)).ReturnsAsync(Forced);
            Model.Setup(x => x.PrepareForceUpdate(later)).ReturnsAsync(forcedLatest);
            Model.Setup(x => x.VerifyRelease(Forced)).ReturnsAsync(false);
            Model.Setup(x => x.DownloadRelease(Forced)).Returns(Task.CompletedTask);
            Model.Setup(x => x.ApplyRelease(Forced)).Returns(Task.CompletedTask);
            Notes.Setup(x => x.GetChangelogAsync(It.IsAny<Version>(), It.IsAny<CultureInfo>(), It.IsAny<CancellationToken>()))
                .Returns<Version, CultureInfo, CancellationToken>((version, _, _) =>
                    Task.FromResult(new ApplicationUpdateChangelog("Selected native " + version, null, null)));
            var changelogs = new Mock<IFactory<INativeApplicationUpdateChangelogProvider>>();
            changelogs.Setup(x => x.Create()).Returns(Notes.Object);
            var messages = new Mock<IMessageBoxService>();
            messages.Setup(x => x.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, string>((title, content) =>
                {
                    Prompts.Add((title, content));
                    return Task.FromResult(Prompts.Count > 1);
                });
            var app = Mock.Of<IAppArguments>(x => x.Version == new Version(2, 0, 0, 0));
            owner = new ApplicationUpdateHandlerRegistrator(sources, Model.Object, app, changelogs.Object, null);
            var ui = new DispatcherScheduler(dispatcher, DispatcherPriority.Normal);
            Updater = new ApplicationUpdaterViewModel(Mock.Of<IFactory<IUpdaterWindowDisplayer>>(), Model.Object, owner,
                Mock.Of<IApplicationAccessor>(), config, messages.Object, ui,
                new DispatcherScheduler(dispatcher, DispatcherPriority.ApplicationIdle), Scheduler.Default);
            Window = new UpdaterWindowViewModel(new UpdaterWindowArgs(), Mock.Of<IWindowViewController>(), app,
                Mock.Of<IApplicationAccessor>(), sources, Updater, Mock.Of<IErrorMonitorViewModel>());
        }

        public Mock<IApplicationUpdaterModel> Model { get; } = new();
        public Mock<INativeApplicationUpdateChangelogProvider> Notes { get; } = new();
        public List<(string Title, string Content)> Prompts { get; } = new();
        public IReleaseEntry Earlier { get; }
        public IPoeUpdateInfo Forced { get; }
        public ApplicationUpdaterViewModel Updater { get; }
        public UpdaterWindowViewModel Window { get; }

        public async Task CheckAsync()
        {
            await dispatcher.InvokeAsync(() => { }, DispatcherPriority.ContextIdle);
            Updater.CheckForUpdatesCommand.CanExecute(null).ShouldBeTrue();
            Updater.CheckForUpdatesCommand.Execute(null);
            await WaitAsync(() => !Updater.IsBusy && Updater.LatestVersion == new Version(3, 0, 0, 0)
                && Window.SelectedReleaseEntry?.Version.Version == new Version(3, 0, 0, 0));
            await dispatcher.InvokeAsync(() => { }, DispatcherPriority.ContextIdle);
            Updater.IsInErrorStatus.ShouldBeFalse();
        }

        public async Task SelectEarlierAsync()
        {
            Window.SelectedReleaseEntry = Earlier;
            await WaitAsync(() => !Updater.IsBusy && ReferenceEquals(Updater.LatestUpdate, Forced));
            await dispatcher.InvokeAsync(() => { }, DispatcherPriority.ContextIdle);
        }

        public void Dispose()
        {
            var failures = new List<Exception>();
            try { Updater.IsBusy.ShouldBeFalse("Actual native operations must drain before disposing their dispatcher"); }
            catch (Exception error) { failures.Add(error); }
            foreach (var scope in new IDisposable[] { Window, Updater, owner, sources, config })
            {
                try { scope.Dispose(); }
                catch (Exception error) { failures.Add(error); }
            }
            if (failures.Count != 0) throw new AggregateException("Owned updater scopes did not cleanly drain/dispose", failures);
        }

        private static IReleaseEntry Release(string version, char hash) =>
            ReleaseEntry.ParseReleaseEntry(new string(hash, 40) + $" EyeAuras-{version}-full.nupkg 1024");

        private static async Task WaitAsync(Func<bool> completed)
        {
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (!completed() && DateTime.UtcNow < deadline) await Task.Delay(10);
            completed().ShouldBeTrue("The supported command and normal native selection must complete");
        }
    }
}
