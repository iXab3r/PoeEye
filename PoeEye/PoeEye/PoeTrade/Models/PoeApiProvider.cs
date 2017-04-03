using System.Linq;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeShared;
using PoeShared.PoeTrade;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeEye.PoeTrade.Models
{
    internal sealed class PoeApiProvider : DisposableReactiveObject, IPoeApiProvider
    {
        public PoeApiProvider(
            [NotNull] IUnityContainer container,
            [NotNull] IFactory<IPoeApiWrapper, IPoeApi> wrapperFactory)
        {
            Guard.ArgumentNotNull(container, nameof(container));
            Guard.ArgumentNotNull(wrapperFactory, nameof(wrapperFactory));

            Log.Instance.Debug($"[PoeApiProvider..ctor] Loading APIs list...");
            var apiList = container
                .ResolveAll<IPoeApi>()
                .Select(wrapperFactory.Create)
                .ToArray();
            Log.Instance.Debug($"[PoeApiProvider..ctor] API list:\r\n\t{apiList.DumpToText()}");

            ModulesList = new ReactiveList<IPoeApiWrapper>(apiList)
            {
                ChangeTrackingEnabled = true,
            };
        }

        public IReactiveList<IPoeApiWrapper> ModulesList { get; }
    }
}
