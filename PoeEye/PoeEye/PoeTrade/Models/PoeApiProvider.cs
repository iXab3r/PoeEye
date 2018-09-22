using System;
using System.Collections.ObjectModel;
using System.Linq;
using Common.Logging;
using DynamicData;
using DynamicData.Binding;
using Guards;
using JetBrains.Annotations;
using PoeShared.PoeTrade;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Unity;

namespace PoeEye.PoeTrade.Models
{
    internal sealed class PoeApiProvider : DisposableReactiveObject, IPoeApiProvider
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PoeApiProvider));

        private readonly ObservableCollectionExtended<IPoeApiWrapper> modulesList = new ObservableCollectionExtended<IPoeApiWrapper>();

        public PoeApiProvider(
            [NotNull] IUnityContainer container,
            [NotNull] IFactory<IPoeApiWrapper, IPoeApi> wrapperFactory)
        {
            Guard.ArgumentNotNull(container, nameof(container));
            Guard.ArgumentNotNull(wrapperFactory, nameof(wrapperFactory));

            Log.Debug("[PoeApiProvider..ctor] Loading APIs list...");
            var apiList = container
                          .ResolveAll<IPoeApi>()
                          .Select(wrapperFactory.Create)
                          .ToObservableCollection();
            Log.Debug($"[PoeApiProvider..ctor] API list:\r\n\t{apiList.Select(x => $"{x.Name} ({x.Id})").DumpToText()}");

            apiList
                .ToObservableChangeSet()
                .Bind(modulesList)
                .Subscribe()
                .AddTo(Anchors);

            ModulesList = new ReadOnlyObservableCollection<IPoeApiWrapper>(modulesList);
        }

        public ReadOnlyObservableCollection<IPoeApiWrapper> ModulesList { get; }
    }
}