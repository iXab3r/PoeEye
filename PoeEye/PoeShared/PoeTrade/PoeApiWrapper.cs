using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Common.Logging;
using Guards;
using JetBrains.Annotations;
using Newtonsoft.Json;
using PoeShared.PoeTrade.Query;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;
using Unity.Attributes;

namespace PoeShared.PoeTrade
{
    internal class PoeApiWrapper : DisposableReactiveObject, IPoeApiWrapper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PoeApiWrapper));

        private readonly IPoeApi api;
        private readonly IPoeStaticDataProvider provider;

        public PoeApiWrapper(
            [NotNull] IPoeApi api,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler,
            [NotNull] IFactory<IPoeStaticDataProvider, IPoeApi> queryInfoFactory)
        {
            Guard.ArgumentNotNull(api, nameof(api));
            Guard.ArgumentNotNull(queryInfoFactory, nameof(queryInfoFactory));

            this.api = api;
            provider = queryInfoFactory.Create(api);

            provider.WhenAnyValue(x => x.IsBusy)
                    .ObserveOn(uiScheduler)
                    .Subscribe(() => this.RaisePropertyChanged(nameof(IsBusy)))
                    .AddTo(Anchors);

            provider.WhenAnyValue(x => x.Error)
                    .ObserveOn(uiScheduler)
                    .Subscribe(() => this.RaisePropertyChanged(nameof(Error)))
                    .AddTo(Anchors);
            
            provider.WhenAnyValue(x => x.StaticData)
                    .ObserveOn(uiScheduler)
                    .Subscribe(() => this.RaisePropertyChanged(nameof(StaticData)))
                    .AddTo(Anchors);

            api.WhenAnyValue(x => x.IsAvailable).ToUnit().Merge(provider.WhenAnyValue(x => x.StaticData).ToUnit())
               .ObserveOn(uiScheduler)
               .Subscribe(() => this.RaisePropertyChanged(nameof(IsAvailable)))
               .AddTo(Anchors);
        }

        public IPoeStaticData StaticData => provider.StaticData;

        public bool IsAvailable => api.IsAvailable && !StaticData.IsEmpty;

        public IObservable<IPoeQueryResult> GetLiveUpdates(IPoeQueryInfo query)
        {
            Guard.ArgumentNotNull(query, nameof(query));
            Log.Debug($"[PoeApiWrapper, {Name}] Issuing live query... {query.DumpToText(Formatting.None)}");

            return api
                .SubscribeToLiveUpdates(query);
        }

        public Task<IPoeQueryResult> IssueQuery(IPoeQueryInfo query)
        {
            Guard.ArgumentNotNull(query, nameof(query));

            Log.Debug($"[PoeApiWrapper, {Name}] Issuing query... {query.DumpToText(Formatting.None)}");

            return api.IssueQuery(query);
        }

        public void DisposeQuery(IPoeQueryInfo query)
        {
            Guard.ArgumentNotNull(query, nameof(query));

            Log.Debug($"[PoeApiWrapper, {Name}] Disposing query... {query.DumpToText(Formatting.None)}");

            api.DisposeQuery(query);
        }

        public bool IsBusy => provider.IsBusy;

        public string Error => provider.Error;

        public string Name => api.Name ?? api.GetType().Name;

        public Guid Id => api.Id;

        public override string ToString()
        {
            return $"Name: {Name}, Api: {api}";
        }
    }
}