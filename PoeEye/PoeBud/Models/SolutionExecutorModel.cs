using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Guards;
using JetBrains.Annotations;
using PoeShared;
using PoeShared.Scaffolding;
using PoeShared.StashApi.DataTypes;

namespace PoeBud.Models
{
    internal sealed class SolutionExecutorModel : ISolutionExecutorModel
    {
        private readonly ISubject<string> logQueue = new Subject<string>();
        private readonly IPoeWindowManager windowManager;

        public SolutionExecutorModel([NotNull] IPoeWindowManager windowManager)
        {
            Guard.ArgumentNotNull(windowManager, nameof(windowManager));

            this.windowManager = windowManager;
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

            Log.Instance.Debug(
                $"[SolutionExecutor.ExecuteSolution] Executing solution: {solutionToExecute.DumpToText()} ...");
            var visibleTabs = solutionToExecute.Tabs.Where(x => !x.hidden).ToArray();
            IStashTab activeTab = null;
            foreach (var item in solutionToExecute.Items.OrderBy(x => x.TabIndex))
            {
                if (activeTab?.Idx != item.TabIndex)
                {
                    logQueue.OnNext($"Switching to tab #{item.TabIndex} ...");

                    var tabToSelect = visibleTabs.First(x => x.Idx == item.TabIndex);
                    window.SelectStashTabByIdx(tabToSelect, visibleTabs);
                    activeTab = tabToSelect;
                }

                logQueue.OnNext($"Transferring item {item.Name}({item.ItemType}) @ X{item.X} Y{item.Y}");

                //FIXME StashTypeName parsing
                window.TransferItemFromStash(
                    item.X, item.Y,
                    activeTab.StashTypeName == "QuadStash" ? StashTabType.QuadStash : StashTabType.NormalStash);
            }

            logQueue.OnNext("Solution was executed successfully");
        }
    }
}
