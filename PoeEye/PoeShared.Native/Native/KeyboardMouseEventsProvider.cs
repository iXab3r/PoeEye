using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using WindowsHook;
using log4net;
using PInvoke;
using PoeShared.Logging;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Unity;

namespace PoeShared.Native
{
    internal sealed class KeyboardMouseEventsProvider : IKeyboardMouseEventsProvider
    {
        private static readonly IFluentLog Log = typeof(KeyboardMouseEventsProvider).PrepareLogger();

        private static readonly object HookGate = new(); //GMA HookHelper uses static variable to temporarily store HookProcedure

        [InjectionConstructor]
        public KeyboardMouseEventsProvider([Dependency(WellKnownSchedulers.InputHook)]
            IScheduler inputScheduler) : this(
            globalEventsFactory: new LambdaFactory<IKeyboardMouseEvents>(() => Hook.CreateGlobalEvents()),
            appEventsFactory: new LambdaFactory<IKeyboardMouseEvents>(() => Hook.CreateAppEvents()),
            inputScheduler)
        {
        }

        internal KeyboardMouseEventsProvider(
            IFactory<IKeyboardMouseEvents> globalEventsFactory,
            IFactory<IKeyboardMouseEvents> appEventsFactory,
            IScheduler inputScheduler)
        {
            System = Observable
                .Using(() => new HookHandler("global", globalEventsFactory, inputScheduler), x => Observable.Return(x.Source).Concat(Observable.Never<IKeyboardMouseEvents>()))
                .SubscribeOn(inputScheduler)
                .Replay(1)
                .RefCount();

            Application = Observable
                .Using(() => new HookHandler("app", appEventsFactory, inputScheduler), x => Observable.Return(x.Source).Concat(Observable.Never<IKeyboardMouseEvents>()))
                .Publish()
                .RefCount();
        }

        public IObservable<IKeyboardMouseEvents> System { get; }

        public IObservable<IKeyboardMouseEvents> Application { get; }

        private sealed class HookHandler : DisposableReactiveObject
        {
            private static long GlobalHookId;

            private readonly int constuctorThreadId;
            private readonly long hookId = Interlocked.Increment(ref GlobalHookId);
            private readonly string name;

            public HookHandler(string name, IFactory<IKeyboardMouseEvents> factory, IScheduler inputScheduler)
            {
                Log.Info($"Creating hook source {this}");
                this.name = name;
                constuctorThreadId = Kernel32.GetCurrentThreadId();
                Source = Hook(name, factory);
                Log.Info($"Created hook source {this} on thread {constuctorThreadId}");
                new ScheduledDisposable(inputScheduler, Disposable.Create(() =>
                {
                    Log.Info($"Disposing hook source {this}");
                    var disposeThreadId = Kernel32.GetCurrentThreadId();
                    if (disposeThreadId != constuctorThreadId)
                    {
                        Log.Warn($"Disposing on an invalid thread, expected {constuctorThreadId}, got {disposeThreadId}");
                        throw new InvalidOperationException($"Disposing hook {this} on an invalid thread, expected {constuctorThreadId}, got {disposeThreadId}");
                    }

                    lock (HookGate)
                    {
                        Source.Dispose();
                    }

                    Log.Info($"Disposed hook source {this}");
                })).AddTo(Anchors);
            }

            public IKeyboardMouseEvents Source { get; }

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

            public override string ToString()
            {
                return $"Hook#{hookId} {name}";
            }
        }
    }
}