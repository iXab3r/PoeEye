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
            Guard.ArgumentNotNull(() => apiProvider);
            this.apiProvider = apiProvider;

            SelectedModule = apiProvider.ModulesList.FirstOrDefault();
        }

        public IReactiveList<IPoeApiWrapper> ModulesList => apiProvider.ModulesList;

        public IPoeApiWrapper SelectedModule
        {
            get { return selectedModule; }
            set { this.RaiseAndSetIfChanged(ref selectedModule, value); }
        }

        public void SetByModuleName(string moduleName)
        {
            Guard.ArgumentNotNull(() => moduleName);

            var module = ModulesList.FirstOrDefault(x => x.Name == moduleName);
            if (module != null)
            {
                SelectedModule = module;
            }
        }
    }
}