// This code is distributed under MIT license. 
// Copyright (c) 2015 George Mamaladze
// See license.txt or https://mit-license.org/

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using App.Metrics;
using App.Metrics.Counter;
using App.Metrics.Gauge;
using App.Metrics.Timer;
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

        private readonly CounterOptions callbackCallCount;
        private readonly TimerOptions callbackTime;

        protected BaseListener(Subscribe subscribe)
        {
            Handle = subscribe(CallbackHook).AddTo(Anchors);
            Log = SharedLog.WithSuffix(ToString);
            var listenerType = GetType();
            callbackCallCount = new CounterOptions
            {
                Name = $"{Handle.HookType} CallCount",
                MeasurementUnit = Unit.Calls,
            };
            callbackTime = new TimerOptions
            {
                Name = $"{Handle.HookType} ProcessingTime",
            };
            Disposable.Create(() => Log.Debug("Disposing listener...")).AddTo(Anchors);
            Log.Debug(() => $"Created new listener of type {listenerType}");
            Disposable.Create(() => Log.Debug("Disposed listener")).AddTo(Anchors);
        }

        private HookResult Handle { get; }

        protected bool IsReady { get; init; }

        protected IFluentLog Log { get; }

        private bool CallbackHook(WinHookCallbackData data)
        {
            if (!IsReady)
            {
                return false;
            }

            using var time = Log.Metrics.Measure.Timer.Time(callbackTime);
            var result =  Callback(data);
            Log.Metrics.Measure.Counter.Increment(callbackCallCount);
            return result;
        }

        protected abstract bool Callback(WinHookCallbackData data);

        public override string ToString()
        {
            return $"Listener Hook: {Handle}{(IsReady ? null : "(not ready)")}";
        }
    }
}