#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace PoeShared.Squirrel.Updater;

/// <summary>
/// Exposes the application's available update sources and effective selection. Explicit configuration
/// distinguishes a persisted disable from legacy/default channel initialization.
/// </summary>
public interface IUpdateSourceProvider : IDisposableReactiveObject
{
    UpdateSourceInfo UpdateSource { get; }
    
    string UpdateSourceId { get; set; }
        
    IReadOnlyList<UpdateSourceInfo> KnownSources { get; set; }

    /// <summary>Whether an intentional source choice or disable supersedes legacy configuration.</summary>
    bool IsSourceSelectionConfigured { get; }

    /// <summary>
    /// Atomically replaces the runtime catalog and persists the intended selection; null explicitly disables updates.
    /// Source objects and handlers are runtime-owned and are not serialized with that choice.
    /// </summary>
    void ConfigureSources(IReadOnlyList<UpdateSourceInfo> sources, string? selectedId);
}
