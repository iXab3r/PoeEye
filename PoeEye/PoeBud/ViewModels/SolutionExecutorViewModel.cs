using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeBud.Models;
using PoeBud.OfficialApi.DataTypes;
using PoeShared;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeBud.ViewModels
{
    internal sealed class SolutionExecutorViewModel : DisposableReactiveObject, ISolutionExecutorViewModel
    {
        private readonly IScheduler bgScheduler;

        private readonly ISubject<string> logQueue = new Subject<string>();
        private readonly IScheduler uiScheduler;
        private readonly IPoeWindowsManager windowsManager;

        private bool isBusy;

        public SolutionExecutorViewModel(
            [NotNull] IPoeWindowsManager windowsManager,
            [NotNull] [Dependency(WellKnownSchedulers.Ui)] IScheduler uiScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            Guard.ArgumentNotNull(() => windowsManager);
            Guard.ArgumentNotNull(() => uiScheduler);
            Guard.ArgumentNotNull(() => bgScheduler);

            this.windowsManager = windowsManager;
            this.uiScheduler = uiScheduler;
            this.bgScheduler = bgScheduler;

            logQueue
                .ObserveOn(uiScheduler)
                .Subscribe(PerformedOperations.Add)
                .AddTo(Anchors);
        }

        public bool IsBusy
        {
            get { return isBusy; }
            set { this.RaiseAndSetIfChanged(ref isBusy, value); }
        }

        public IReactiveList<string> PerformedOperations { get; } = new ReactiveList<string>();

        public async Task ExecuteSolution(IPoeTradeSolution solutionToExecute)
        {
            PerformedOperations.Clear();
            IsBusy = true;
            await Task.Run(() => ExecuteSolutionInternal(solutionToExecute));
            IsBusy = false;
        }

        private void ExecuteSolutionInternal(IPoeTradeSolution solutionToExecute)
        {
            Guard.ArgumentNotNull(() => solutionToExecute);

            var window = windowsManager.ActiveWindow;
            if (window == null)
            {
                throw new ApplicationException("PoE window is not active atm");
            }

            logQueue.OnNext("Executing solution...");

            Log.Instance.Debug(
                $"[SolutionExecutor.ExecuteSolution] Executing solution: {solutionToExecute.DumpToText()} ...");
            var visibleTabs = solutionToExecute.Tabs.Where(x => !x.hidden).ToArray();
            ITab activeTab = null;
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

                //FIXME StashTypeName parsig
                window.TransferItemFromStash(
                    item.X, item.Y,
                    activeTab.StashTypeName == "QuadStash" ? StashTabType.QuadStash : StashTabType.NormalStash);
            }

            logQueue.OnNext("Solution was executed successfully");

            Log.Instance.Debug(
                $"[SolutionExecutor.ExecuteSolution] Log:\r\n\t{string.Join("\r\n\t", PerformedOperations)}");
        }
    }
}