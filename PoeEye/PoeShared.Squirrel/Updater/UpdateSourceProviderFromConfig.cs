using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.Squirrel.Updater;

/// <summary>
/// Resolves legacy channel defaults while preserving intentional selection in app-owned runtime metadata.
/// Catalog objects stay in memory; a persisted null is an explicit disable, never a fallback request.
/// </summary>
internal sealed class UpdateSourceProviderFromConfig : DisposableReactiveObject, IUpdateSourceProvider
{
    private static readonly IFluentLog Log = typeof(UpdateSourceProviderFromConfig).PrepareLogger();
    private readonly IConfigProvider<UpdateSettingsConfig> configProvider;
    private readonly string selectionFilePath;
    private readonly bool awaitAuthorUpdateConfiguration;
    private readonly object gate = new();
    private IReadOnlyList<UpdateSourceInfo> knownSources;
    private string updateSourceId;
    private UpdateSourceInfo updateSource;
    private bool isSourceSelectionConfigured;
    private bool isSaving;
    private bool isAuthorConfigurationCompleted;
    private UpdateSourceSelection selection;

    // One immutable identity covers effective source and configuration, including A -> B -> A.
    internal UpdateSourceSelection Selection => Volatile.Read(ref selection);
    internal event Action SelectionChanged;

    /// <summary>
    /// Uses the application's profile when available. Legacy direct construction without app arguments
    /// retains in-memory selection and never guesses a filesystem location.
    /// </summary>
    public UpdateSourceProviderFromConfig(IConfigProvider<UpdateSettingsConfig> configProvider,
        IAppArguments appArguments = null)
    {
        this.configProvider = configProvider;
        awaitAuthorUpdateConfiguration = configProvider.ActualConfig.AwaitAuthorUpdateConfiguration;
        if (!string.IsNullOrWhiteSpace(appArguments?.AppDataDirectory))
            selectionFilePath = Path.Combine(appArguments.AppDataDirectory, ".updater", "selection.json");

        updateSourceId = configProvider.ActualConfig.UpdateSourceId;
        if (selectionFilePath != null && File.Exists(selectionFilePath))
        {
            var selection = JObject.Parse(File.ReadAllText(selectionFilePath));
            isSourceSelectionConfigured = selection.Value<bool>("configured");
            if (isSourceSelectionConfigured)
                updateSourceId = selection.Value<string>("selectedSourceId");
        }

        this.selection = new UpdateSourceSelection(isSourceSelectionConfigured, updateSourceId, null);

        configProvider.ListenTo(x => x.UpdateSourceId)
            .SubscribeSafe(id =>
            {
                lock (gate)
                {
                    if (!isSourceSelectionConfigured && !isSaving)
                        ApplySelection(knownSources, id, intentional: false);
                }
            }, Log.HandleUiException)
            .AddTo(Anchors);
    }

    public UpdateSourceInfo UpdateSource => updateSource;

    public bool IsSourceSelectionConfigured => isSourceSelectionConfigured;

    // These setters perform persistence and commit all related fields before INPC; independent
    // auto-property notifications would expose an intermediate catalog/selection to consumers.
    [PropertyChanged.DoNotNotify]
    public string UpdateSourceId
    {
        get => updateSourceId;
        set
        {
            lock (gate) ApplySelection(knownSources, value, intentional: true);
        }
    }

    [PropertyChanged.DoNotNotify]
    public IReadOnlyList<UpdateSourceInfo> KnownSources
    {
        get => knownSources;
        set
        {
            lock (gate)
                ApplySelection(value, value?.Count > 0 ? updateSourceId : null,
                    intentional: value == null || value.Count == 0);
        }
    }

    public void ConfigureSources(IReadOnlyList<UpdateSourceInfo> sources, string selectedId) =>
        ApplySelection(sources, sources?.Count > 0 ? selectedId : null, intentional: true, completeAuthorConfiguration: true);

    private void ApplySelection(IReadOnlyList<UpdateSourceInfo> sources, string selectedId, bool intentional, bool completeAuthorConfiguration = false)
    {
        lock (gate)
        {
            var nextSources = sources?.ToArray();
            var awaitingAuthor = awaitAuthorUpdateConfiguration && !isAuthorConfigurationCompleted && !completeAuthorConfiguration;
            var nextSource = awaitingAuthor ? null : nextSources?.FirstOrDefault(x => x.Id == selectedId);
            if (!awaitingAuthor && !intentional && !isSourceSelectionConfigured && nextSource == null && nextSources?.Length > 0)
            {
                nextSource = nextSources[0];
                selectedId = nextSource.Id;
            }

            if (intentional && selectionFilePath != null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(selectionFilePath));
                var temporaryFile = selectionFilePath + ".tmp";
                var metadata = new JObject
                {
                    ["configured"] = true,
                    ["selectedSourceId"] = selectedId == null ? JValue.CreateNull() : new JValue(selectedId)
                };
                File.WriteAllText(temporaryFile, metadata.ToString(Formatting.None));
                File.Move(temporaryFile, selectionFilePath, overwrite: true);
            }

            var sourcesChanged = !ReferenceEquals(knownSources, sources);
            var idChanged = updateSourceId != selectedId;
            var configuredChanged = intentional && !isSourceSelectionConfigured;
            var sourceChanged = !Equals(updateSource, nextSource);
            // Commit author readiness only after persistence, together with selection before notifications.
            isAuthorConfigurationCompleted |= completeAuthorConfiguration;
            knownSources = nextSources;
            updateSourceId = selectedId;
            isSourceSelectionConfigured |= intentional;
            updateSource = nextSource;
            Volatile.Write(ref selection, new UpdateSourceSelection(isSourceSelectionConfigured, updateSourceId,
                updateSource == null ? null : updateSource with { Uris = updateSource.Uris?.ToArray() }));

            isSaving = true;
            try
            {
                if (configProvider.ActualConfig.UpdateSourceId != selectedId)
                    configProvider.Save(configProvider.ActualConfig with { UpdateSourceId = selectedId });
            }
            finally
            {
                isSaving = false;
                // Runtime metadata is authoritative even if mirroring the legacy config fails.
                SelectionChanged?.Invoke();
                if (sourcesChanged) this.RaisePropertyChanged(nameof(KnownSources));
                if (idChanged) this.RaisePropertyChanged(nameof(UpdateSourceId));
                if (configuredChanged) this.RaisePropertyChanged(nameof(IsSourceSelectionConfigured));
                if (sourceChanged) this.RaisePropertyChanged(nameof(UpdateSource));
            }
        }
    }
}

/// <summary>App-owned source values and a unique selection identity, committed before consumer notifications.</summary>
internal sealed record UpdateSourceSelection(bool IsConfigured, string SelectedId, UpdateSourceInfo Source);
