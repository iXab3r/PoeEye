using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
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
        private readonly ISubject<string> logQueue = new Subject<string>();
        private readonly IPoeWindowManager windowManager;
        private readonly IHighlightingService highlightingService;

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
            Guard.ArgumentNotNull(solutionToExecute, nameof(solutionToExecute));
            await Task.Run(() => ExecuteSolutionInternal(solutionToExecute));
        }

        private void ExecuteSolutionInternal(IPoeTradeSolution solutionToExecute)
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
                Log.Instance.Debug(
                    $"[SolutionExecutor.ExecuteSolution] Executing solution: {solutionToExecute.DumpToText()} ...");
                var visibleTabs = solutionToExecute.Tabs.Where(x => !x.Hidden).ToArray();
                IStashTab activeTab = null;
                foreach (var item in solutionToExecute.Items.OrderBy(x => x.Tab.Idx))
                {
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
