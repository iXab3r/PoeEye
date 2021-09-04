// This code is distributed under MIT license. 
// Copyright (c) 2015 George Mamaladze
// See license.txt or https://mit-license.org/

using System.Reactive.Disposables;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using WindowsHook.WinApi;

namespace WindowsHook.Implementation
{
    internal abstract class BaseListener : DisposableReactiveObject
    {
        private static readonly IFluentLog SharedLog = typeof(BaseListener).PrepareLogger();

        protected BaseListener(Subscribe subscribe)
        {
            Handle = subscribe(CallbackHook).AddTo(Anchors);
            Log = SharedLog.WithSuffix(ToString);
            Disposable.Create(() => Log.Debug("Disposing listener...")).AddTo(Anchors);
            Log.Debug($"Created new listener of type {GetType()}");
            Disposable.Create(() => Log.Debug("Disposed listener")).AddTo(Anchors);
        }

        private HookResult Handle { get; }

        protected bool IsReady { get; init; }
        
        protected IFluentLog Log { get; }

        private bool CallbackHook(CallbackData data)
        {
            if (!IsReady)
            {
                return false;
            }

            return Callback(data);
        }

        protected abstract bool Callback(CallbackData data);

        public override string ToString()
        {
            return $"Listener Hook: {Handle}, IsReady: {IsReady}, ";
        }
    }
}