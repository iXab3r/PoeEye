using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using WindowsHook;
using PInvoke;
using PoeShared.Logging;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Unity;

namespace PoeShared.Native;

internal sealed class KeyboardMouseEventsProvider : IKeyboardMouseEventsProvider
{
    private static readonly IFluentLog Log = typeof(KeyboardMouseEventsProvider).PrepareLogger();

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
        System = new HookHandlerContainer(() => new HookHandler("global", globalEventsFactory))
            .Source
            .SubscribeOn(inputScheduler);

        Application = new HookHandlerContainer(() => new HookHandler("app", appEventsFactory))
            .Source
            .SubscribeOn(inputScheduler);
    }

    public IObservable<IKeyboardMouseEvents> System { get; }

    public IObservable<IKeyboardMouseEvents> Application { get; }

    /// <summary>
    /// Basically it is Using + Replay(1) + RefCount
    /// The problem was that Using + Replay(1) are not working as needed - Replay returns resource even if it was already disposed
    /// </summary>
    private sealed class HookHandlerContainer : DisposableReactiveObject
    {
        private static readonly IFluentLog Log = typeof(HookHandlerContainer).PrepareLogger();

        private readonly Func<HookHandler> factoryFunc;
        private readonly object hookGate = new();

        private HookHandler activeValue;
        private int refCount;

        public HookHandlerContainer(Func<HookHandler> factoryFunc)
        {
            this.factoryFunc = factoryFunc;
                
            //FIXME Extract to a separate RX operator - ShareReplay or ShareUsing ? 
            Source = Observable.Create<IKeyboardMouseEvents>(observer =>
            {
                var activeAnchors = new CompositeDisposable();

                Disposable.Create(() =>
                {
                    lock (hookGate)
                    {
                        Log.Info($"Processed unsubscription, refCount: {refCount} => {refCount - 1}");
                        if (--refCount > 0)
                        {
                            Log.Info($"Preserving value as we still have {refCount} subs left, value: {activeValue}");
                            return;
                        }

                        if (activeValue == null)
                        {
                            throw new InvalidOperationException("Something went wrong - hook handler is not created");
                        }
                        
                        Log.Info($"Disposing value as we don't have subs left: {activeValue}");
                        activeValue.Dispose();
                        activeValue = null;
                    }
                }).AddTo(activeAnchors);

                lock (hookGate)
                {
                    Log.Info($"Initializing new subscription, refCount: {refCount} => {refCount+1}");
                    refCount++;
                    if (activeValue == null)
                    {
                        Log.Info("Creating new value");
                        activeValue = factoryFunc();
                        Log.Info($"Created value: {activeValue}");
                    }

                    if (activeValue == null)
                    {
                        throw new InvalidOperationException("Failed to get non-null value from factory function");
                    }
                    observer.OnNext(activeValue.Source);
                }

                return activeAnchors;
            });
        }

        public int RefCount => refCount;

        public IObservable<IKeyboardMouseEvents> Source { get; }
    }

    private sealed class HookHandler : DisposableReactiveObject
    {
        private static long GlobalHookId;

        private readonly int constuctorThreadId;
        private readonly long hookId = Interlocked.Increment(ref GlobalHookId);
        private readonly string name;

        public HookHandler(string name, IFactory<IKeyboardMouseEvents> factory)
        {
            Log.Info($"Creating hook source {this}");
            this.name = name;
            constuctorThreadId = Kernel32.GetCurrentThreadId();
            Source = Hook(name, factory);
            Log.Info($"Created hook source {this} on thread {constuctorThreadId}");
            Disposable.Create(() =>
            {
                Log.Info($"Disposing hook source {this}");
                var disposeThreadId = Kernel32.GetCurrentThreadId();
                if (disposeThreadId != constuctorThreadId)
                {
                    Log.Warn($"Disposing on an invalid thread, expected {constuctorThreadId}, got {disposeThreadId}");
                    throw new InvalidOperationException($"Disposing hook {this} on an invalid thread, expected {constuctorThreadId}, got {disposeThreadId}");
                }
                Source.Dispose();
                Log.Info($"Disposed hook source {this}");
            }).AddTo(Anchors);
        }

        public IKeyboardMouseEvents Source { get; }

        private static IKeyboardMouseEvents Hook(string name, IFactory<IKeyboardMouseEvents> factory)
        {
            Log.Info($"[{name}] Performing input events hook");
            Log.Info($"[{name}] Getting {nameof(IKeyboardMouseEvents)} source");
            var events = factory.Create();
            Log.Info($"[{name}] Got {nameof(IKeyboardMouseEvents)} source: {events}");
            return events;
        }
        
        protected override void FormatToString(ToStringBuilder builder)
        {
            base.FormatToString(builder);
            builder.Append("Hook");
            builder.AppendParameter(nameof(hookId), hookId);
            builder.AppendParameter(nameof(name), name);
            builder.AppendParameter(nameof(Source), Source);
        }
    }
}