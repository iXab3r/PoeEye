using System;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DynamicData;
using Guards;
using JetBrains.Annotations;
using PoeShared;
using PoeShared.Common;
using PoeShared.PoeTrade;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;
using Unity.Attributes;

namespace PoeEye.PoeTrade.ViewModels
{
    internal class PoeItemTypeSelectorViewModel : DisposableReactiveObject, IPoeItemTypeSelectorViewModel
    {
        private readonly ReadOnlyObservableCollection<string> knownTypes;
        private readonly SourceCache<IPoeItemType, string> knownTypesByName;

        private string selectedValue;

        public PoeItemTypeSelectorViewModel(
            [NotNull] IPoeStaticDataSource staticDataSource,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(staticDataSource, nameof(staticDataSource));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));

            knownTypesByName = new SourceCache<IPoeItemType, string>(x => x.Name);
            knownTypesByName.Connect()
                            .Transform(x => x.Name)
                            .ObserveOn(uiScheduler)
                            .Bind(out knownTypes)
                            .Subscribe()
                            .AddTo(Anchors);

            staticDataSource
                .WhenAnyValue(x => x.StaticData)
                .ObserveOn(uiScheduler)
                .Subscribe(staticData =>
                {
                    knownTypesByName.Edit(updater =>
                    {
                        updater.Clear();
                        staticData.ItemTypes.ForEach(updater.AddOrUpdate);
                    });
                })
                .AddTo(Anchors);
        }

        public string SelectedValue
        {
            get => selectedValue;
            set => this.RaiseAndSetIfChanged(ref selectedValue, value);
        }

        public ReadOnlyObservableCollection<string> KnownItemTypes => knownTypes;

        public IPoeItemType ToItemType()
        {
            if (string.IsNullOrWhiteSpace(SelectedValue))
            {
                return null;
            }
            var knownType = knownTypesByName.Lookup(SelectedValue);
            if (knownType.HasValue)
            {
                return knownType.Value;
            }

            Log.Instance.Warn($"Failed to find ItemType '{SelectedValue}' in types cache, known items: {knownTypes.DumpToTextRaw()}");
            return null;
        }
    }
}