using PoeShared.Scaffolding;

namespace PoeShared.Bindings
{
    public interface IReactiveBinding : IDisposableReactiveObject
    {
        string TargetPropertyPath { get; }
        
        IValueWatcher SourceWatcher { get; }
        
        IValueWatcher TargetWatcher { get; }
        
        bool IsActive { get; }
    }
}