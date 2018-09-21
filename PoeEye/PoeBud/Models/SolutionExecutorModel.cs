using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Guards;
using JetBrains.Annotations;
using PoeBud.Scaffolding;
using PoeBud.Services;
using PoeShared;
using PoeShared.Scaffolding;
using PoeShared.StashApi.DataTypes;

namespace PoeBud.Models
{
    internal sealed class SolutionExecutorModel : ISolutionExecutorModel
    {
        private static readonly ILog Log = LogManager.GetLogger<SolutionExecutorModel>();
        
        private readonly IHighlightingService highlightingService;
        private readonly ISubject<string> logQueue = new Subject<string>();
        private readonly IPoeWindowManager windowManager;

        public SolutionExecutorModel(
            [NotNull] IPoeWindowManager windowManager,
            [NotNull] IHighlightingService highlightingService)
        {
            Guard.ArgumentNotNull(windowManager, nameof(windowManager));

            this.windowManager = windowManager;
            this.highlightingService = highlightingService;
        }

        public IObservable<string> Messages => logQueue.AsObservable();

        public async Task ExecuteSolution(IPoeTradeSolution solutionToExecute)
        {
            var tokenSource = new CancellationTokenSource();
            await ExecuteSolution(solutionToExecute, tokenSource.Token);
        }

        public async Task ExecuteSolution(IPoeTradeSolution solutionToExecute, CancellationToken token)
        {
            Guard.ArgumentNotNull(solutionToExecute, nameof(solutionToExecute));

            await Task.Run(() => ExecuteSolutionInternal(solutionToExecute, token), token);
        }

        private void ExecuteSolutionInternal(IPoeTradeSolution solutionToExecute, CancellationToken token)
        {
            Guard.ArgumentNotNull(solutionToExecute, nameof(solutionToExecute));

            var window = windowManager.ActiveWindow;
            if (window == null)
            {
                throw new ApplicationException("PoE window is not active atm");
            }

            logQueue.OnNext("Executing solution...");

            using (highlightingService.Highlight(solutionToExecute))
            {
                Log.Debug(
                    $"[SolutionExecutor.ExecuteSolution] Executing solution: {solutionToExecute.DumpToText()} ...");
                var visibleTabs = solutionToExecute.Tabs.Where(x => !x.Hidden).ToArray();
                IStashTab activeTab = null;
                foreach (var item in solutionToExecute.Items.OrderBy(x => x.Tab.Idx))
                {
                    token.ThrowIfCancellationRequested();

                    if (activeTab?.Idx != item.Tab.Idx)
                    {
                        logQueue.OnNext($"Switching to tab '{item.Tab.Name}'(idx: #{item.Tab.Idx}, inventoryId: {item.Tab.GetInventoryId()}) ...");

                        window.SelectStashTabByIdx(item.Tab, visibleTabs);
                        activeTab = item.Tab;
                    }

                    logQueue.OnNext($"Transferring item {item.Name}({item.ItemType}) @ {item.Position}");

                    window.TransferItemFromStash(
                        item.Position.X, item.Position.Y,
                        activeTab.StashType);
                }

                logQueue.OnNext("Solution was executed successfully");
            }
        }
    }
}