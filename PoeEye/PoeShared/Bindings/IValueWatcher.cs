using PoeShared.Scaffolding;

namespace PoeShared.Bindings
{
    public interface IValueWatcher : IDisposableReactiveObject
    {
        bool HasValue { get; }
        
        object Source { get; set; }
        
        object Value { get; }
        
        bool CanSetValue { get; }
        
        void SetCurrentValue(object newValue);
    }
}