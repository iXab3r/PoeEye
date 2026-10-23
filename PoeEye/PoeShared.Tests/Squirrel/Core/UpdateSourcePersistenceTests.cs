using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Squirrel.Prism;
using PoeShared.Squirrel.Updater;
using Shouldly;
using Unity;

namespace PoeShared.Tests.Squirrel.Core;

/// <summary>
/// Supplemental A01/C03/C05 contract coverage using real config serialization and files.
/// The configuration-only app arguments point into one owned temporary profile; this does not
/// establish process startup, installed/Squirrel isolation, cancellation or self-update proof.
/// </summary>
[TestFixture]
[Category("Integration")]
[NonParallelizable]
public sealed class UpdateSourcePersistenceTests
{
    private string root;
    private ProfileArguments arguments;

    [SetUp]
    public void SetUp()
    {
        root = Path.Combine(Path.GetTempPath(), "EyeAuras-Updater-Tdd", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        arguments = new ProfileArguments(root, "updater-" + Guid.NewGuid().ToString("N"));
    }

    [TearDown]
    public void TearDown()
    {
        if (TestContext.CurrentContext.Result.FailCount != 0)
        {
            typeof(UpdateSourcePersistenceTests).PrepareLogger().Info($"Retaining owned config evidence: {root}");
            return;
        }
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }

    /// <summary>
    /// WHAT: A01 legacy source choice, updater preferences and companion user data survive catalog initialization.
    /// HOW: Save actual combined config, recreate the container over that profile, populate the ordinary catalog
    /// through the public provider and inspect both effective selection and serialized settings.
    /// </summary>
    [Test]
    public void ShouldRestoreLegacySelectionWithoutResettingConfiguration()
    {
        // Given
        var expected = new UpdateSettingsConfig
        {
            UpdateSourceId = "alpha", AutoUpdateTimeout = TimeSpan.FromMinutes(17),
            AutomaticallyDownloadUpdates = true, IgnoreDeltaUpdates = false,
            PreferredChangelogLanguageId = "ru", Version = 11
        };
        using (var seed = CreateContainer())
        {
            seed.Resolve<IConfigProvider<UpdateSettingsConfig>>().Save(expected);
            seed.Resolve<IConfigProviderFromFile>().Save(new UserState { Value = "keep-me" });
        }

        // When
        using var boot = CreateContainer();
        var sources = boot.Resolve<IUpdateSourceProvider>();
        sources.KnownSources = Catalog();

        // Then
        sources.UpdateSourceId.ShouldBe("alpha");
        sources.UpdateSource.Id.ShouldBe("alpha");
        boot.Resolve<IConfigProvider<UpdateSettingsConfig>>().ActualConfig.ShouldBe(expected);
        boot.Resolve<IConfigProviderFromFile>().GetActualConfig<UserState>().Value.ShouldBe("keep-me");
        sources.IsSourceSelectionConfigured.ShouldBeFalse("A legacy stored ID is not a new explicit selection record");
    }

    /// <summary>
    /// WHAT: C03 clearing the public source catalog intentionally disables updates and stays disabled on next boot.
    /// HOW: Select an ordinary source, clear KnownSources, recreate all services over the real saved profile,
    /// restore the available catalog and require effective null selection rather than legacy fallback.
    /// </summary>
    [Test]
    public void ShouldKeepPublicCatalogDisableAcrossServiceRestart()
    {
        // Given
        using (var first = CreateContainer())
        {
            var sources = first.Resolve<IUpdateSourceProvider>();
            sources.KnownSources = Catalog();
            sources.UpdateSourceId = "alpha";
            first.Resolve<IConfigProviderFromFile>().Save(new UserState { Value = "keep-me" });

            // When
            sources.KnownSources = Array.Empty<UpdateSourceInfo>();

            // Then
            sources.UpdateSource.ShouldBeNull();
            sources.UpdateSourceId.ShouldBeNull();
            sources.IsSourceSelectionConfigured.ShouldBeTrue();
            AssertRuntimeSelection(null);
        }

        // Given: a subsequent publisher-supplied combined config still carries the old channel.
        // Writing this file is setup; effective runtime metadata owns the explicit disable.
        using (var legacyWriter = CreateContainer())
        {
            var config = legacyWriter.Resolve<IConfigProvider<UpdateSettingsConfig>>();
            config.Save(config.ActualConfig with { UpdateSourceId = "alpha" });
        }

        // When
        using var next = CreateContainer();
        var restored = next.Resolve<IUpdateSourceProvider>();
        restored.KnownSources = Catalog();

        // Then
        restored.UpdateSource.ShouldBeNull("An explicit disable must not fall back to the first available source");
        restored.UpdateSourceId.ShouldBeNull("The public getter reports effective persisted selection");
        restored.IsSourceSelectionConfigured.ShouldBeTrue();
        AssertRuntimeSelection(null);
        next.Resolve<IConfigProviderFromFile>().GetActualConfig<UserState>().Value.ShouldBe("keep-me");
    }

    /// <summary>
    /// WHAT: C03/C05 routine author configuration retains persisted disable; intentional public selection re-enables.
    /// HOW: Configure no sources, recreate the provider, use its configured/null getters in the supported bootstrap
    /// pattern, then choose a source through UpdateSourceId and verify that choice after another service restart.
    /// </summary>
    [Test]
    public void ShouldRetainDisableDuringRoutineConfigurationAndPersistIntentionalReenable()
    {
        // Given
        using (var first = CreateContainer())
            first.Resolve<IUpdateSourceProvider>().ConfigureSources(Array.Empty<UpdateSourceInfo>(), null);

        // When
        using (var routineBoot = CreateContainer())
        {
            var sources = routineBoot.Resolve<IUpdateSourceProvider>();
            var selected = sources.IsSourceSelectionConfigured ? sources.UpdateSourceId : "stable";
            sources.ConfigureSources(Catalog(), selected);

            // Then
            sources.UpdateSourceId.ShouldBeNull();
            sources.UpdateSource.ShouldBeNull();

            // When
            sources.UpdateSourceId = "stable";

            // Then
            sources.UpdateSourceId.ShouldBe("stable");
            sources.UpdateSource.Id.ShouldBe("stable");
            routineBoot.Resolve<IConfigProvider<UpdateSettingsConfig>>().ActualConfig.UpdateSourceId.ShouldBe("stable");
        }

        // When
        using var finalBoot = CreateContainer();
        var restored = finalBoot.Resolve<IUpdateSourceProvider>();
        restored.ConfigureSources(Catalog(), restored.UpdateSourceId);

        // Then
        restored.IsSourceSelectionConfigured.ShouldBeTrue();
        restored.UpdateSourceId.ShouldBe("stable");
        restored.UpdateSource.Id.ShouldBe("stable");
    }

    /// <summary>
    /// WHAT: A01/C05 a new ordinary profile keeps the existing first-source default until an explicit selection exists.
    /// HOW: Resolve the public provider from standard registrations over a fresh real profile and load its catalog;
    /// compare effective source and configuration before choosing another channel intentionally.
    /// </summary>
    [Test]
    public void ShouldKeepOrdinaryDefaultAndPersistExplicitChannelChoice()
    {
        // Given
        using (var container = CreateContainer())
        {
        var sources = container.Resolve<IUpdateSourceProvider>();

        // When
        sources.KnownSources = Catalog();

        // Then
        sources.UpdateSource.Id.ShouldBe("stable");
        sources.UpdateSourceId.ShouldBe("stable");
        sources.IsSourceSelectionConfigured.ShouldBeFalse("Internal legacy default seeding is not intentional user selection");
        container.Resolve<IConfigProvider<UpdateSettingsConfig>>().ActualConfig.Version.ShouldBe(11);

        // When
        sources.UpdateSourceId = "alpha";

        // Then
        sources.UpdateSource.Id.ShouldBe("alpha");
        sources.IsSourceSelectionConfigured.ShouldBeTrue();
        container.Resolve<IConfigProvider<UpdateSettingsConfig>>().ActualConfig.UpdateSourceId.ShouldBe("alpha");
        AssertRuntimeSelection("alpha");
        }

        // Given: the pack's combined config may contain its former default rather than the user's choice.
        using (var legacyWriter = CreateContainer())
        {
            var config = legacyWriter.Resolve<IConfigProvider<UpdateSettingsConfig>>();
            config.Save(config.ActualConfig with { UpdateSourceId = "stable" });
        }

        // When
        using var next = CreateContainer();
        var restored = next.Resolve<IUpdateSourceProvider>();
        restored.KnownSources = Catalog();

        // Then
        restored.IsSourceSelectionConfigured.ShouldBeTrue();
        restored.UpdateSourceId.ShouldBe("alpha", "Runtime selection takes precedence over publisher combined config");
        restored.UpdateSource.Id.ShouldBe("alpha");
        AssertRuntimeSelection("alpha");
    }

    private void AssertRuntimeSelection(string selectedId)
    {
        var file = Path.Combine(arguments.AppDataDirectory, ".updater", "selection.json");
        File.Exists(file).ShouldBeTrue("Explicit updater selection has an app-owned persistence boundary");
        var metadata = JObject.Parse(File.ReadAllText(file));
        metadata.GetValue("configured", StringComparison.OrdinalIgnoreCase).Value<bool>().ShouldBeTrue();
        var selected = metadata.GetValue("selectedSourceId", StringComparison.OrdinalIgnoreCase);
        selected.ShouldNotBeNull("Null selection is an explicit persisted value, not a missing field");
        selected.Value<string>().ShouldBe(selectedId);
    }

    private IUnityContainer CreateContainer()
    {
        var container = new UnityContainer();
        container.AddExtension(new CommonRegistrations());
        container.AddExtension(new UpdaterRegistrations());
        container.RegisterInstance<IAppArguments>(arguments);
        var files = container.Resolve<IConfigProviderFromFile>();
        container.RegisterInstance<IConfigProvider>(files);
        files.Reload();
        return container;
    }

    private static IReadOnlyList<UpdateSourceInfo> Catalog() => new[]
    {
        new UpdateSourceInfo { Id = "stable", Name = "Stable", Uris = new[] { "https://example.invalid/stable/" } },
        new UpdateSourceInfo { Id = "alpha", Name = "Alpha", Uris = new[] { "https://example.invalid/alpha/" } }
    };

    /// <summary>Companion config proves updater preferences do not reset unrelated persisted user state.</summary>
    public sealed record UserState : IPoeEyeConfig
    {
        public string Value { get; init; }
    }

    /// <summary>Configuration-only host boundary; every persistent path belongs to the same owned temporary profile.</summary>
    private sealed class ProfileArguments : IAppArguments
    {
        public ProfileArguments(string root, string profile)
        {
            Profile = profile;
            DataFolder = root;
            RoamingAppDataDirectory = root;
            LocalAppDataDirectory = root;
            AppDataDirectory = Path.Combine(root, profile);
            TempDirectory = Path.Combine(AppDataDirectory, "temp");
            EnvironmentAppData = new DirectoryInfo(root);
            EnvironmentLocalAppData = new DirectoryInfo(root);
        }

        public string AppName => "UpdaterContractFixture";
        public string AppTitle => AppName;
        public Version Version => new(1, 0);
        public string AppDomainDirectory => AppDomain.CurrentDomain.BaseDirectory;
        public string AppDataDirectory { get; }
        public string TempDirectory { get; }
        public string RoamingAppDataDirectory { get; }
        public string LocalAppDataDirectory { get; }
        public int ProcessId => Environment.ProcessId;
        public string ApplicationExecutableName => Path.GetFileName(ApplicationExecutablePath);
        public string ApplicationExecutablePath => Environment.ProcessPath;
        public DirectoryInfo EnvironmentLocalAppData { get; }
        public DirectoryInfo EnvironmentAppData { get; }
        public string Profile { get; }
        public string DataFolder { get; }
        public bool IsDebugMode => false;
        public bool IsLazyMode => false;
        public bool ShowUpdater => false;
        public bool? IsSafeMode => null;
        public bool? IsAdminMode => null;
        public IEnumerable<string> PrismModules => Array.Empty<string>();
        public bool IsElevated => false;
        public string AutostartFlag => null;
        public string StartupArgs => string.Empty;
        public string[] CommandLineArguments => Array.Empty<string>();
        public string[] CommandLineArgumentsEx => Array.Empty<string>();
        public string[] CompositeCommandLineArguments => Array.Empty<string>();
        public bool Parse(string[] args) => throw new NotSupportedException("Configuration-only fixture");
        public string[] ParseCommandLineArguments(string commandLine) => throw new NotSupportedException("Configuration-only fixture");
    }
}
