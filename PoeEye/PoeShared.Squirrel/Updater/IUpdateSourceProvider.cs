using System.Collections.Generic;
using System.Collections.ObjectModel;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace PoeShared.Squirrel.Updater;

public interface IUpdateSourceProvider : IDisposableReactiveObject
{
    UpdateSourceInfo UpdateSource { get; }
    
    string UpdateSourceId { get; set; }
        
    IReadOnlyList<UpdateSourceInfo> KnownSources { get; set; }
}