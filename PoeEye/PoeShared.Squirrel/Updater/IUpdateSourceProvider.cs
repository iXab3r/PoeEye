using System.Collections.ObjectModel;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace PoeShared.Squirrel.Updater;

public interface IUpdateSourceProvider : IDisposableReactiveObject
{
    UpdateSourceInfo UpdateSource { get; set; }
        
    ReadOnlyObservableCollection<UpdateSourceInfo> KnownSources { [NotNull] get; }

    void AddSource(UpdateSourceInfo sourceInfo);
}