using System.Collections.Concurrent;
using PoeShared.Prism;

namespace PoeShared.Native
{
    internal sealed class WinEventHookWrapperFactory : IFactory<IWinEventHookWrapper, WinEventHookArguments>
    {
        private readonly IFactory<WinEventHookWrapper, WinEventHookArguments> winEventHookWrapperFactory;
        private readonly ConcurrentDictionary<WinEventHookArguments, IWinEventHookWrapper> hooks = new();

        public WinEventHookWrapperFactory(IFactory<WinEventHookWrapper, WinEventHookArguments> winEventHookWrapperFactory)
        {
            this.winEventHookWrapperFactory = winEventHookWrapperFactory;
        }

        public IWinEventHookWrapper Create(WinEventHookArguments param1)
        {
            //FIXME These hooks are never disposed
            return hooks.GetOrAdd(param1, arg => winEventHookWrapperFactory.Create(arg));
        }
    }
}