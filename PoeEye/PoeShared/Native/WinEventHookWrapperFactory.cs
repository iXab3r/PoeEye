using System.Collections.Concurrent;
using PoeShared.Prism;

namespace PoeShared.Native
{
    internal sealed class WinEventHookWrapperFactory : IFactory<IWinEventHookWrapper, WinEventHookArguments>
    {
        private readonly ConcurrentDictionary<WinEventHookArguments, IWinEventHookWrapper> hooks = new ConcurrentDictionary<WinEventHookArguments, IWinEventHookWrapper>();
        
        public IWinEventHookWrapper Create(WinEventHookArguments param1)
        {
            return hooks.GetOrAdd(param1, arg => new WinEventHookWrapper(arg));
        }
    }
}