using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Guards;
using JetBrains.Annotations;
using PoeEye.PoeTrade.Models;
using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;
using Unity.Attributes;

namespace PoeEye.PoeTrade.ViewModels
{
    public sealed class PoeApiSelectorViewModel : DisposableReactiveObject, IPoeApiSelectorViewModel, IPoeStaticDataSource
    {
        private readonly IPoeApiProvider apiProvider;
        private IPoeApiWrapper selectedModule;

        public PoeApiSelectorViewModel(
            [NotNull] IPoeApiProvider apiProvider,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(apiProvider, nameof(apiProvider));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));
            this.apiProvider = apiProvider;

            this.WhenAnyValue(x => x.SelectedModule)
                .Select(x => x == null ? Observable.Return(Unit.Default).Concat(Observable.Never<Unit>()) : x.WhenAnyValue(y => y.StaticData).ToUnit())
                .Switch()
                .ObserveOn(uiScheduler)
                .Subscribe(() => this.RaisePropertyChanged(nameof(StaticData)))
                .AddTo(Anchors);
        }

        public ReadOnlyObservableCollection<IPoeApiWrapper> ModulesList => apiProvider.ModulesList;

        public IPoeApiWrapper SelectedModule
        {
            get => selectedModule;
            set => this.RaiseAndSetIfChanged(ref selectedModule, value);
        }

        public IPoeApiWrapper SetByModuleId(string moduleIdOrName)
        {
            return SelectedModule = FindModuleById(moduleIdOrName);
        }

        public IPoeStaticData StaticData => SelectedModule == null ? PoeStaticData.Empty : SelectedModule.StaticData;

        private IPoeApiWrapper FindModuleById(string moduleInfo)
        {
            Guid moduleId;
            if (Guid.TryParse(moduleInfo, out moduleId))
            {
                return ModulesList.FirstOrDefault(x => x.Id == moduleId);
            }

            return ModulesList.FirstOrDefault(x => x.Name == moduleInfo);
        }
    }
}