using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Gma.System.MouseKeyHook;
using log4net;
using PoeShared.Logging;
using PoeShared.Prism;
using PoeShared.Scaffolding; 
using PoeShared.Logging;

namespace PoeShared.Native
{
    internal sealed class KeyboardMouseEventsProvider : IKeyboardMouseEventsProvider
    {
        private static readonly IFluentLog Log = typeof(KeyboardMouseEventsProvider).PrepareLogger();

        private static readonly object HookGate = new(); //GMA HookHelper uses static variable to temporarily store HookProcedure

        public KeyboardMouseEventsProvider() : this(
            globalEventsFactory: new LambdaFactory<IKeyboardMouseEvents>(Gma.System.MouseKeyHook.Hook.GlobalEvents),
            appEventsFactory: new LambdaFactory<IKeyboardMouseEvents>(Gma.System.MouseKeyHook.Hook.AppEvents))
        {
        }
        
        internal KeyboardMouseEventsProvider(IFactory<IKeyboardMouseEvents> globalEventsFactory, IFactory<IKeyboardMouseEvents> appEventsFactory)
        {
            System = Observable
                .Using(() => Hook("global", globalEventsFactory),x => Observable.Return(x).Concat(Observable.Never<IKeyboardMouseEvents>()))
                .Do(x => Log.Debug($"Test"), ex => { },() => Log.Debug($"Completed"))
                .Replay(1)
                .RefCount()
                .Do(x => Log.Debug($"Test 1"), ex => { },() => Log.Debug($"Completed 1"));
            
            Application = Observable
                .Using(() => Hook("app", appEventsFactory), x => Observable.Return(x).Concat(Observable.Never<IKeyboardMouseEvents>()))
                .Publish()
                .RefCount();
        }

        private static IKeyboardMouseEvents Hook(string name, IFactory<IKeyboardMouseEvents> factory)
        {
            Log.Info($"[{name}] Performing input events hook");
            lock (HookGate)
            {
                Log.Info($"[{name}] Getting {nameof(IKeyboardMouseEvents)} source");
                var events = factory.Create();
                Log.Info($"[{name}] Got {nameof(IKeyboardMouseEvents)} source: {events}");
                return events;
            }
        }

        public IObservable<IKeyboardMouseEvents> System { get; }

        public IObservable<IKeyboardMouseEvents> Application { get; }
    }
}