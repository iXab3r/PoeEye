using System;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Threading.Tasks;
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
            IErrorMonitorViewModel errorMonitor,
            [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler,
            [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            ReportProblemCommand = errorMonitor.ReportProblemCommand;
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
            ThrowInsideTaskCommand = CommandWrapper.Create(() =>
            {
                Log.Debug("Throwing inside task");
                Task.Run(() =>
                {
                    Log.Debug("Throwing exception");
                    throw new ApplicationException("Exception that was thrown inside Task");
                });
            });
        }

        public ICommand ThrowOnUiSchedulerCommand { get; }
        public ICommand ThrowOnBgSchedulerCommand { get; }
        public ICommand ThrowInsideTaskCommand { get; }
        public ICommand ReportProblemCommand { get; }

        public ICommand ThrowInsideCommand { get; }
    }
}