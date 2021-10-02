﻿using System;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Windows.Input;
using PoeShared.Logging;
using PoeShared.Prism;
using PropertyBinder;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using Unity;

namespace PoeShared.UI
{
    internal sealed class ExceptionSandboxViewModel : DisposableReactiveObject
    {
        private static readonly IFluentLog Log = typeof(ExceptionSandboxViewModel).PrepareLogger();

        private readonly ISubject<Exception> sinkThrow = new Subject<Exception>();
        private readonly ISubject<Exception> sinkThrowOnUiScheduler = new Subject<Exception>();
        private readonly ISubject<Exception> sinkThrowOnBgScheduler = new Subject<Exception>();

        public ExceptionSandboxViewModel(
            [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler,
            [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            ThrowInsideCommand = CommandWrapper.Create(() =>
            {
                Log.Debug("Throwing exception");
                throw new ApplicationException("Exception that was thrown inside command");
            });
            ThrowOnUiSchedulerCommand = CommandWrapper.Create(() =>
            {
                Log.Debug("Scheduling on UI");
                uiScheduler.Schedule(() =>
                {
                    Log.Debug("Throwing exception");
                    throw new ApplicationException("Exception that was thrown on UI scheduler");
                });
            });
            ThrowOnBgSchedulerCommand = CommandWrapper.Create(() =>
            {
                Log.Debug("Scheduling on BG");
                bgScheduler.Schedule(() =>
                {
                    Log.Debug("Throwing exception");
                    throw new ApplicationException("Exception that was thrown on BG scheduler");
                });
            });
        }

        public ICommand ThrowOnUiSchedulerCommand { get; }
        public ICommand ThrowOnBgSchedulerCommand { get; }

        public ICommand ThrowInsideCommand { get; }
    }
}