using System;
using System.Linq;
using Guards;
using JetBrains.Annotations;
using PoeEye.PoeTrade.Models;
using PoeShared.PoeTrade;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeEye.PoeTrade.ViewModels
{
    public sealed class PoeApiSelectorViewModel : DisposableReactiveObject, IPoeApiSelectorViewModel
    {
        private readonly IPoeApiProvider apiProvider;
        private IPoeApiWrapper selectedModule;

        public PoeApiSelectorViewModel([NotNull] IPoeApiProvider apiProvider)
        {
            Guard.ArgumentNotNull(apiProvider, nameof(apiProvider));
            this.apiProvider = apiProvider;

            SelectedModule = apiProvider.ModulesList.First();
        }

        public IReactiveList<IPoeApiWrapper> ModulesList => apiProvider.ModulesList;

        public IPoeApiWrapper SelectedModule
        {
            get { return selectedModule; }
            set { this.RaiseAndSetIfChanged(ref selectedModule, value); }
        }

        public void SetByModuleId(string moduleInfo)
        {
            Guard.ArgumentNotNull(moduleInfo, nameof(moduleInfo));

            SelectedModule = FindModuleById(moduleInfo);
        }

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
