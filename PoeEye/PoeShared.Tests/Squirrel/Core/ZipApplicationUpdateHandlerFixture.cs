using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using PoeShared.Scaffolding;
using PoeShared.Squirrel.Updater;
using Shouldly;

namespace PoeShared.Tests.Squirrel.Core;

/// <summary>
/// Unit collaboration coverage of the public ZIP base forwarder, with a deliberately controlled service.
/// Real ZIP preparation is covered separately by the application automation; installation is not exercised here.
/// </summary>
[TestFixture]
public sealed class ZipApplicationUpdateHandlerFixture
{
    /// <summary>
    /// WHAT: Explicit preparation and reprepare forward the last successfully prepared opaque token and current Apply arguments.
    /// HOW: Drive public base Check/Download/Apply over equal-valued targets, three fresh descriptors and controlled service Tasks.
    /// This sequential positive workflow does not establish wrong-target rejection or old-token exclusion during pending reprepare.
    /// </summary>
    [Test]
    public async Task ShouldForwardLatestPreparedTokenAndAwaitServiceTasks()
    {
        // Given
        var first = new Target("installed", "revision");
        var second = new Target("installed", "revision");
        first.ShouldBe(second);
        first.ShouldNotBeSameAs(second);
        var artifacts = new[] { Artifact('1'), Artifact('2'), Artifact('3') };
        var service = new ControlledZipService();
        var handler = new AuthorProtocol(service, new[] { first, second }, artifacts);
        var source = new UpdateSourceInfo { Id = "author", Uris = new[] { "https://example.invalid/author/" } };
        var prepareProgress = Mock.Of<IProgressReporter>();
        var applyProgress = Mock.Of<IProgressReporter>();
        using var prepareCancellation = new CancellationTokenSource();
        using var applyCancellation = new CancellationTokenSource();
        var started = new List<Task>();
        try
        {
            // When
            var firstTarget = await handler.CheckAsync(source, prepareCancellation.Token);
            firstTarget.ShouldBeSameAs(first);
            var firstDownload = handler.DownloadAsync(firstTarget, prepareProgress, prepareCancellation.Token);
            started.Add(firstDownload);
            await firstDownload.WaitAsync(TimeSpan.FromSeconds(5));

            // Then
            service.PreparedArtifacts.Count.ShouldBe(1);
            service.PreparedArtifacts[0].ShouldBeSameAs(artifacts[0]);
            handler.ResolvedTargets[0].ShouldBeSameAs(first);

            // When
            var secondTarget = await handler.CheckAsync(source, prepareCancellation.Token);
            secondTarget.ShouldBeSameAs(second);
            var secondDownload = handler.DownloadAsync(secondTarget, prepareProgress, prepareCancellation.Token);
            started.Add(secondDownload);
            await secondDownload.WaitAsync(TimeSpan.FromSeconds(5));

            // Then
            service.PreparedArtifacts.Count.ShouldBe(2);
            service.PreparedArtifacts[1].ShouldBeSameAs(artifacts[1]);
            handler.ResolvedTargets[1].ShouldBeSameAs(second);

            // When
            var reprepare = handler.DownloadAsync(secondTarget, prepareProgress, prepareCancellation.Token);
            started.Add(reprepare);
            await service.ReprepareEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

            // Then
            reprepare.IsCompleted.ShouldBeFalse("Public Download must await the actual third Prepare Task");
            service.PreparedArtifacts.Count.ShouldBe(3);
            service.PreparedArtifacts[2].ShouldBeSameAs(artifacts[2]);
            handler.ResolvedTargets[2].ShouldBeSameAs(second);
            service.PrepareReporters.ShouldAllBe(x => ReferenceEquals(x, prepareProgress));
            service.PrepareTokens.ShouldAllBe(x => x == prepareCancellation.Token);

            // When
            service.ReprepareGate.TrySetResult(service.Third);
            await reprepare.WaitAsync(TimeSpan.FromSeconds(5));
            var apply = handler.ApplyAsync(secondTarget, applyProgress, applyCancellation.Token);
            started.Add(apply);
            await service.ApplyEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

            // Then
            service.ApplyCalls.ShouldBe(1);
            service.Applied.ShouldBeSameAs(service.Third);
            service.Applied.ShouldNotBeSameAs(service.First);
            service.Applied.ShouldNotBeSameAs(service.Second);
            service.ApplyReporter.ShouldBeSameAs(applyProgress);
            service.ApplyToken.ShouldBe(applyCancellation.Token);
            handler.ResolvedTargets.Count.ShouldBe(3, "Apply must not resolve or prepare again");
            service.PreparedArtifacts.Count.ShouldBe(3);
            apply.IsCompleted.ShouldBeFalse("Public Apply must await the actual service Apply Task");

            // When
            service.ApplyGate.TrySetResult(true);
            await apply.WaitAsync(TimeSpan.FromSeconds(5));

            // Then
            service.ApplyCalls.ShouldBe(1);
            service.PreparedArtifacts.Count.ShouldBe(3);
            handler.ResolvedTargets.Count.ShouldBe(3);
        }
        finally
        {
            service.ReprepareGate.TrySetResult(service.Third);
            service.ApplyGate.TrySetResult(true);
            var drain = Task.WhenAll(started);
            try
            {
                await drain.WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch when (drain.IsCompleted)
            {
                // Actual tasks have terminated; retain the original workflow failure rather than replacing it during drain.
            }
        }
    }

    private static ZipUpdateArtifact Artifact(char id) =>
        new(new Uri($"https://example.invalid/publication/{id}.zip"), new string(id, 64), 100);

    private sealed record Target(string InstalledVersionText, string TargetVersionText) : IApplicationUpdateInfo
    {
        public bool IsUpdateAvailable => true;
        public bool IsDowngrade => false;
        public long? DownloadSize => 100;
    }

    private sealed class AuthorProtocol : ZipApplicationUpdateHandler
    {
        private readonly Queue<IApplicationUpdateInfo> targets;
        private readonly Queue<ZipUpdateArtifact> artifacts;

        public AuthorProtocol(IApplicationZipUpdateService service, IEnumerable<IApplicationUpdateInfo> targets,
            IEnumerable<ZipUpdateArtifact> artifacts) : base(service)
        {
            this.targets = new Queue<IApplicationUpdateInfo>(targets);
            this.artifacts = new Queue<ZipUpdateArtifact>(artifacts);
        }

        public List<IApplicationUpdateInfo> ResolvedTargets { get; } = new();

        protected override Task<IApplicationUpdateInfo> CheckSourceAsync(UpdateSourceInfo source, CancellationToken cancellationToken) =>
            Task.FromResult(targets.Dequeue());

        protected override Task<ApplicationUpdateChangelog> ReadChangelogAsync(IApplicationUpdateInfo update, CultureInfo culture,
            CancellationToken cancellationToken) => Task.FromResult(new ApplicationUpdateChangelog("notes", null, null));

        protected override Task<ZipUpdateArtifact> ResolveArtifactAsync(IApplicationUpdateInfo update, IProgressReporter progress,
            CancellationToken cancellationToken)
        {
            ResolvedTargets.Add(update);
            return Task.FromResult(artifacts.Dequeue());
        }
    }

    /// <summary>Only a unit service double: these friend-constructed tokens are not verified files or runnable releases.</summary>
    private sealed class ControlledZipService : IApplicationZipUpdateService
    {
        public ControlledZipService()
        {
            First = new PreparedZipUpdate(new string('1', 64), 100, "unit-token-one", this);
            Second = new PreparedZipUpdate(new string('2', 64), 100, "unit-token-two", this);
            Third = new PreparedZipUpdate(new string('3', 64), 100, "unit-token-three", this);
        }

        public PreparedZipUpdate First { get; }
        public PreparedZipUpdate Second { get; }
        public PreparedZipUpdate Third { get; }
        public TaskCompletionSource<PreparedZipUpdate> ReprepareGate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> ReprepareEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> ApplyGate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> ApplyEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public List<ZipUpdateArtifact> PreparedArtifacts { get; } = new();
        public List<IProgressReporter> PrepareReporters { get; } = new();
        public List<CancellationToken> PrepareTokens { get; } = new();
        public int ApplyCalls { get; private set; }
        public PreparedZipUpdate Applied { get; private set; }
        public IProgressReporter ApplyReporter { get; private set; }
        public CancellationToken ApplyToken { get; private set; }

        public Task<PreparedZipUpdate> PrepareAsync(ZipUpdateArtifact artifact, IProgressReporter progress, CancellationToken cancellationToken)
        {
            PreparedArtifacts.Add(artifact);
            PrepareReporters.Add(progress);
            PrepareTokens.Add(cancellationToken);
            if (PreparedArtifacts.Count == 1) return Task.FromResult(First);
            if (PreparedArtifacts.Count == 2) return Task.FromResult(Second);
            ReprepareEntered.TrySetResult(true);
            return ReprepareGate.Task;
        }

        public Task ApplyAsync(PreparedZipUpdate prepared, IProgressReporter progress, CancellationToken cancellationToken)
        {
            ApplyCalls++;
            Applied = prepared;
            ApplyReporter = progress;
            ApplyToken = cancellationToken;
            ApplyEntered.TrySetResult(true);
            return ApplyGate.Task;
        }
    }
}
