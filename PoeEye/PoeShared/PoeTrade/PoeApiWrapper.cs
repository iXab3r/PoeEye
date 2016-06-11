using System.Threading.Tasks;
using Guards;
using JetBrains.Annotations;
using PoeShared.PoeTrade.Query;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.PoeTrade
{
    internal class PoeApiWrapper : DisposableReactiveObject, IPoeApiWrapper
    {
        private readonly IPoeApi api;
        private readonly PoeQueryInfoProvider provider;

        public PoeApiWrapper(
            [NotNull] IPoeApi api,
            [NotNull] IFactory<PoeQueryInfoProvider, IPoeApi> queryInfoFactory)
        {
            Guard.ArgumentNotNull(() => api);
            Guard.ArgumentNotNull(() => queryInfoFactory);

            this.api = api;
            provider = queryInfoFactory.Create(api);

            provider
                .WhenAnyValue(x => x.IsBusy)
                .Subscribe(() => this.RaisePropertyChanged(nameof(IsBusy)))
                .AddTo(Anchors);
        }

        public IPoeStaticData StaticData => provider;

        public Task<IPoeQueryResult> IssueQuery(IPoeQueryInfo query)
        {
            Guard.ArgumentNotNull(() => query);
            return api.IssueQuery(query);
        }

        public bool IsBusy => provider.IsBusy;

        public string Name => api.Name ?? api.GetType().Name;

        public override string ToString()
        {
            return $"Name: {Name}, Api: {api}";
        }
    }
}