using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Guards;
using JetBrains.Annotations;
using PoeShared.Common;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.PoeTrade.Query
{
    internal sealed class PoeQueryInfoProvider : DisposableReactiveObject
    {
        private readonly IPoeApi poeApi;
        private bool isBusy;
        private IPoeStaticData staticData = PoeStaticData.Empty;

        public PoeQueryInfoProvider([NotNull] IPoeApi poeApi)
        {
            Guard.ArgumentNotNull(poeApi, nameof(poeApi));
            this.poeApi = poeApi;

            RefreshData();
        }

        public IPoeStaticData StaticData
        {
            get { return staticData; }
            set { this.RaiseAndSetIfChanged(ref staticData, value); }
        } 

        public bool IsBusy
        {
            get { return isBusy; }
            set { this.RaiseAndSetIfChanged(ref isBusy, value); }
        }

        private void RefreshData()
        {
            Log.Instance.Debug($"[PoeQueryInfoProvider-{poeApi.Name}] Refreshing static data...");
            Task.Factory.StartNew(() => StaticData = RefreshDataInternal());
        }

        private IPoeStaticData RefreshDataInternal()
        {
            try
            {
                IsBusy = true;

                Log.Instance.Debug($"[PoeQueryInfoProvider-{poeApi.Name}] Requesting static data from API...");

                var queryResult = poeApi.RequestStaticData().Result;
                
                Log.Instance.Debug($"[PoeQueryInfoProvider-{poeApi.Name}] Received static data from API");
                return queryResult;
            }
            catch (Exception ex)
            {
                Log.HandleUiException(ex);
                Log.Instance.Debug($"[PoeQueryInfoProvider-{poeApi.Name}] Returning empty static data");

                return PoeStaticData.Empty;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
