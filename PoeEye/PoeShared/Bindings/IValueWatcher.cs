using System;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace PoeShared.Bindings
{
    public interface IValueWatcher : IDisposableReactiveObject
    {
        bool HasValue { get; }
        
        object Source { [UsedImplicitly] get; set; }
        
        object Value { get; }
        
        Exception Error { get; }
        
        bool CanSetValue { get; }
        
        bool SupportsSetValue { get; }
        
        void SetCurrentValue(object newValue);
    }
}