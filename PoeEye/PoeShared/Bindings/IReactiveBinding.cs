using System;
using PoeShared.Scaffolding;

namespace PoeShared.Bindings
{
    public interface IReactiveBinding : IDisposableReactiveObject
    {
        /// <summary>
        ///  Currently it's property path - could be nested
        /// </summary>
        string Key { get; }
        
        string Error { get; }
        
        IValueWatcher SourceWatcher { get; }
        
        IValueWatcher TargetWatcher { get; }
        
        bool IsActive { get; }
    }
}