using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Guards;
using JetBrains.Annotations;
using PoeShared.Common;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.PoeTrade.Query
{
    internal sealed class PoeQueryInfoProvider : DisposableReactiveObject, IPoeStaticData
    {
        private readonly Lazy<IPoeStaticData> lazyDataLoader;
        private readonly IPoeApi poeApi;
        private bool isBusy;

        public PoeQueryInfoProvider([NotNull] IPoeApi poeApi)
        {
            Guard.ArgumentNotNull(() => poeApi);
            this.poeApi = poeApi;

            lazyDataLoader = new Lazy<IPoeStaticData>(RefreshData, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public IPoeItemType[] ItemTypes => lazyDataLoader.Value.ItemTypes;

        public string[] LeaguesList => lazyDataLoader.Value.LeaguesList;

        public IPoeCurrency[] CurrenciesList => lazyDataLoader.Value.CurrenciesList;

        public IPoeItemMod[] ModsList => lazyDataLoader.Value.ModsList;

        public bool IsBusy
        {
            get { return isBusy; }
            set { this.RaiseAndSetIfChanged(ref isBusy, value); }
        }

        private IPoeStaticData RefreshData()
        {
            try
            {
                IsBusy = true;

                var queryResult = poeApi.RequestStaticData().Result;
                return queryResult;
            }
            catch (Exception ex)
            {
                Log.HandleUiException(ex);
                return new PoeStaticData();
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}