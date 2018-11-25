using System;
using System.Threading.Tasks;
using Common.Logging;
using Guards;
using JetBrains.Annotations;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.PoeTrade.Query
{
    internal sealed class PoeStaticDataProvider : DisposableReactiveObject, IPoeStaticDataProvider
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PoeStaticDataProvider));

        private readonly IPoeApi poeApi;

        private string error;
        private bool isBusy;
        private IPoeStaticData staticData = PoeStaticData.Empty;

        public PoeStaticDataProvider([NotNull] IPoeApi poeApi)
        {
            Guard.ArgumentNotNull(poeApi, nameof(poeApi));
            this.poeApi = poeApi;

            RefreshData();
        }

        public IPoeStaticData StaticData
        {
            get => staticData;
            set => this.RaiseAndSetIfChanged(ref staticData, value);
        }

        public bool IsBusy
        {
            get => isBusy;
            set => this.RaiseAndSetIfChanged(ref isBusy, value);
        }

        public string Error
        {
            get => error;
            set => this.RaiseAndSetIfChanged(ref error, value);
        }

        private void RefreshData()
        {
            Log.Debug($"[PoeQueryInfoProvider-{poeApi.Name}] Refreshing static data...");
            Task.Factory.StartNew(() => StaticData = RefreshDataInternal());
        }

        private IPoeStaticData RefreshDataInternal()
        {
            try
            {
                IsBusy = true;
                Error = null;

                Log.Debug($"[PoeQueryInfoProvider-{poeApi.Name}] Requesting static data from API...");

                var queryResult = poeApi.RequestStaticData().Result;

                Log.Debug($"[PoeQueryInfoProvider-{poeApi.Name}] Received static data from API");
                return queryResult;
            }
            catch (Exception ex)
            {
                Log.HandleException(ex);
                Log.Debug($"[PoeQueryInfoProvider-{poeApi.Name}] Returning empty static data");
                Error = ex.Message;
                return PoeStaticData.Empty;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}