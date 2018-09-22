﻿using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Common.Logging;
using Guards;
using JetBrains.Annotations;
using PoeBud.Config;
using PoeBud.Models;
using PoeBud.Services;
using PoeShared;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;
using Unity.Attributes;

namespace PoeBud.ViewModels
{
    internal sealed class SolutionExecutorViewModel : DisposableReactiveObject, ISolutionExecutorViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SolutionExecutorViewModel));

        private readonly IClock clock;
        private readonly IHighlightingService highlightingService;
        private readonly IKeyboardEventsSource keyboardMouseEvents;
        private readonly IConfigProvider<PoeBudConfig> poeBudConfigProvider;
        private readonly ISolutionExecutorModel solutionExecutor;
        private readonly IScheduler uiScheduler;

        private bool isBusy;

        public SolutionExecutorViewModel(
            [NotNull] IClock clock,
            [NotNull] ISolutionExecutorModel solutionExecutor,
            [NotNull] IKeyboardEventsSource keyboardMouseEvents,
            [NotNull] IHighlightingService highlightingService,
            [NotNull] IConfigProvider<PoeBudConfig> poeBudConfigProvider,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            Guard.ArgumentNotNull(clock, nameof(clock));
            Guard.ArgumentNotNull(solutionExecutor, nameof(solutionExecutor));
            Guard.ArgumentNotNull(keyboardMouseEvents, nameof(keyboardMouseEvents));
            Guard.ArgumentNotNull(highlightingService, nameof(highlightingService));
            Guard.ArgumentNotNull(poeBudConfigProvider, nameof(poeBudConfigProvider));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));
            Guard.ArgumentNotNull(bgScheduler, nameof(bgScheduler));

            this.clock = clock;
            this.solutionExecutor = solutionExecutor;
            this.keyboardMouseEvents = keyboardMouseEvents;
            this.highlightingService = highlightingService;
            this.poeBudConfigProvider = poeBudConfigProvider;
            this.uiScheduler = uiScheduler;

            solutionExecutor
                .Messages
                .ObserveOn(uiScheduler)
                .Subscribe(LogMessage)
                .AddTo(Anchors);
        }

        public bool IsBusy
        {
            get => isBusy;
            set => this.RaiseAndSetIfChanged(ref isBusy, value);
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

            var cancellationTokenSource = new CancellationTokenSource();
            var cancelSolutionHotkey = new KeyGesture(Key.Escape);
            var keyboardAnchorDisposable = keyboardMouseEvents
                                           .WhenKeyDown
                                           .Where(x => IsBusy)
                                           .Where(x => cancelSolutionHotkey.MatchesHotkey(x))
                                           .Do(x => x.Handled = true)
                                           .ObserveOn(uiScheduler)
                                           .Subscribe(
                                               () =>
                                               {
                                                   Log.Debug("[SolutionExecutor.ExecuteSolution] Cancellation requested");

                                                   LogMessage("Requesting solution cancellation...");
                                                   cancellationTokenSource.Cancel();
                                               });

            try
            {
                IsBusy = true;

                using (keyboardAnchorDisposable)
                {
                    LogMessage("Press ESC at any moment to Cancel");
                    await solutionExecutor.ExecuteSolution(solutionToExecute, cancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
                Log.Debug("[SolutionExecutor.ExecuteSolution] Solution execution was cancelled");
                LogMessage("Solution execution was cancelled");
            }
            finally
            {
                IsBusy = false;
            }

            Log.Debug($"[SolutionExecutor.ExecuteSolution] Log:\r\n\t{string.Join("\r\n\t", PerformedOperations)}");
        }

        private void LogMessage(string logMessage)
        {
            Guard.ArgumentNotNull(logMessage, nameof(logMessage));

            PerformedOperations.Add($"[{clock.Now:HH:mm:ss}] {logMessage}");
        }
    }
}