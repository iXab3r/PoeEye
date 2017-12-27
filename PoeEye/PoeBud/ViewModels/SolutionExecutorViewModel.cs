﻿using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeBud.Models;
using PoeShared;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeBud.ViewModels
{
    internal sealed class SolutionExecutorViewModel : DisposableReactiveObject, ISolutionExecutorViewModel
    {
        private readonly IClock clock;
        private readonly ISolutionExecutorModel solutionExecutor;

        private bool isBusy;

        public SolutionExecutorViewModel(
            [NotNull] IClock clock,
            [NotNull] ISolutionExecutorModel solutionExecutor,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            Guard.ArgumentNotNull(clock, nameof(clock));
            Guard.ArgumentNotNull(solutionExecutor, nameof(solutionExecutor));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));
            Guard.ArgumentNotNull(bgScheduler, nameof(bgScheduler));

            this.clock = clock;
            this.solutionExecutor = solutionExecutor;

            solutionExecutor
                .Messages
                .ObserveOn(uiScheduler)
                .Subscribe(LogMessage)
                .AddTo(Anchors);
        }

        public bool IsBusy
        {
            get { return isBusy; }
            set { this.RaiseAndSetIfChanged(ref isBusy, value); }
        }

        public IReactiveList<string> PerformedOperations { get; } = new ReactiveList<string>();
        
        public void LogOperation(string logMessage)
        {
            Guard.ArgumentNotNull(logMessage, nameof(logMessage));

            PerformedOperations.Clear();

            IsBusy = true;
            LogMessage(logMessage);
            IsBusy = false;
        }

        public async Task ExecuteSolution(IPoeTradeSolution solutionToExecute)
        {
            PerformedOperations.Clear();

            IsBusy = true;
            await solutionExecutor.ExecuteSolution(solutionToExecute);
            IsBusy = false;

            Log.Instance.Debug($"[SolutionExecutor.ExecuteSolution] Log:\r\n\t{string.Join("\r\n\t", PerformedOperations)}");
        }
        
        private void LogMessage(string logMessage)
        {
            Guard.ArgumentNotNull(logMessage, nameof(logMessage));

            PerformedOperations.Add($"[{clock.Now:HH:mm:ss}] {logMessage}");
        }
    }
}
