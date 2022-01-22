using System;
using PoeShared.Scaffolding;

namespace PoeShared.Bindings;

public interface IReactiveBinding : IDisposableReactiveObject
{
    /// <summary>
    ///  Currently it's property path - could be nested
    /// </summary>
    string TargetPropertyPath { get; }
        
    string Error { get; }
        
    IValueProvider SourceWatcher { get; }
        
    IValueWatcher TargetWatcher { get; }
        
    bool IsActive { get; }
}