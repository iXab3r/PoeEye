using System;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace PoeShared.Bindings
{
    public interface IValueProvider : IDisposableReactiveObject
    {
        bool HasValue { get; }
        
        object Value { get; }
        
        Exception Error { get; }
    }
    
    public interface IValueWatcher : IValueProvider
    {
        object Source { [UsedImplicitly] get; set; }
        
        bool CanSetValue { get; }
        
        bool SupportsSetValue { get; }
        
        void SetCurrentValue(object newValue);
    }
}