using System;
using PoeShared.Scaffolding;

namespace PoeShared.Bindings
{
    public interface IReactiveBinding : IDisposableReactiveObject
    {
        string Key { get; }
        
        string Error { get; }
        
        IValueWatcher SourceWatcher { get; }
        
        IValueWatcher TargetWatcher { get; }
        
        bool IsActive { get; }
    }
}