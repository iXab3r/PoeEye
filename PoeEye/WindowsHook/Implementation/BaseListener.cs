// This code is distributed under MIT license. 
// Copyright (c) 2015 George Mamaladze
// See license.txt or https://mit-license.org/

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using PoeShared;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using WindowsHook.WinApi;

namespace WindowsHook.Implementation
{
    internal abstract class BaseListener : DisposableReactiveObject
    {
        private static readonly IFluentLog SharedLog = typeof(BaseListener).PrepareLogger();

        private static readonly TimeSpan ReportingPeriodForLongSubscriber = TimeSpan.FromSeconds(30);
        private static readonly long MaxCallbackTimeMs = 10;

        private long callbackCallCount = 0;
        private long callbackMaxTimeMs = 0;
        private long callbackSpikeCount = 0;
        private long callbackTimeMs = 0;

        protected BaseListener(Subscribe subscribe)
        {
            Handle = subscribe(CallbackHook).AddTo(Anchors);
            Log = SharedLog.WithSuffix(ToString);
            Disposable.Create(() => Log.Debug("Disposing listener...")).AddTo(Anchors);
            Log.Debug($"Created new listener of type {GetType()}");
            Observable.Timer(DateTimeOffset.Now, TimeSpan.FromSeconds(30))
                .SubscribeSafe(_ =>
                {
                    var avg = AvgCallbackTime.TotalMilliseconds;
                    if (avg > MaxCallbackTimeMs)
                    {
                        Log.Warn(() => $"Slow subscriber detected, avg: {avg:F1}ms, max: {callbackMaxTimeMs}ms, call count: {callbackCallCount}, spike count: {callbackSpikeCount}");
                    }
                    else if (callbackCallCount > 0)
                    {
                        Log.Debug(() => $"Reporting hook stats - avg: {avg:F1}ms, max: {callbackMaxTimeMs}ms, call count: {callbackCallCount}, spike count: {callbackSpikeCount}");
                    }
                }, Log.HandleUiException)
                .AddTo(Anchors);
            Disposable.Create(() => Log.Debug("Disposed listener")).AddTo(Anchors);
        }

        private HookResult Handle { get; }

        protected bool IsReady { get; init; }

        protected IFluentLog Log { get; }

        public long CallbackCount => callbackCallCount;

        public TimeSpan AvgCallbackTime => callbackCallCount > 0 ? TimeSpan.FromMilliseconds(callbackTimeMs / (double)callbackCallCount) : TimeSpan.Zero;

        private bool CallbackHook(WinHookCallbackData data)
        {
            if (!IsReady)
            {
                return false;
            }
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result =  Callback(data);
            sw.Stop();

            var elapsedMs = sw.ElapsedMilliseconds;
            callbackCallCount++;
            callbackTimeMs += elapsedMs;
            if (elapsedMs > callbackMaxTimeMs)
            {
                callbackMaxTimeMs = callbackTimeMs;
            }
            if (elapsedMs > MaxCallbackTimeMs)
            {
                callbackSpikeCount++;
            }
            return result;
        }

        protected abstract bool Callback(WinHookCallbackData data);

        public override string ToString()
        {
            return $"Listener Hook: {Handle}{(IsReady ? null : "(not ready)")}";
        }
    }
}