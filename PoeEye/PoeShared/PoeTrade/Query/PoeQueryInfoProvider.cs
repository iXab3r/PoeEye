using System;
using Guards;
using JetBrains.Annotations;
using PoeShared.Common;

namespace PoeShared.PoeTrade.Query
{
    internal sealed class PoeQueryInfoProvider : IPoeStaticData
    {
        private readonly Lazy<IPoeStaticData> lazyDataLoader;
        private readonly IPoeApi poeApi;

        public PoeQueryInfoProvider([NotNull] IPoeApi poeApi)
        {
            Guard.ArgumentNotNull(() => poeApi);
            this.poeApi = poeApi;

            lazyDataLoader = new Lazy<IPoeStaticData>(RefreshData);
        }

        public IPoeItemType[] ItemTypes => lazyDataLoader.Value.ItemTypes;

        public string[] LeaguesList => lazyDataLoader.Value.LeaguesList;

        public IPoeCurrency[] CurrenciesList => lazyDataLoader.Value.CurrenciesList;

        public IPoeItemMod[] ModsList => lazyDataLoader.Value.ModsList;    

        private IPoeStaticData RefreshData()
        {
            try
            {
                var queryResult = poeApi.RequestStaticData().Result;
                return queryResult;
            }
            catch (Exception ex)
            {
                Log.HandleUiException(ex);
                return new PoeStaticData();
            }
        }
    }
}