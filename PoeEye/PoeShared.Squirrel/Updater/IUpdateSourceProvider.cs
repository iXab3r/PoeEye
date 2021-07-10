using System.Collections.Generic;
using System.Collections.ObjectModel;
using JetBrains.Annotations;
using PoeShared.Scaffolding; 
using PoeShared.Logging;

namespace PoeShared.Squirrel.Updater
{
    public interface IUpdateSourceProvider : IDisposableReactiveObject
    {
        UpdateSourceInfo UpdateSource { get; set; }
        
        ReadOnlyObservableCollection<UpdateSourceInfo> KnownSources { [NotNull] get; }

        void AddSource(UpdateSourceInfo sourceInfo);
    }
}