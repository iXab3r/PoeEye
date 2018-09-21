using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DynamicData.Binding;
using Guards;
using JetBrains.Annotations;
using PoeEye.PoeTrade.Models;
using PoeShared.Common;
using PoeShared.PoeDatabase;
using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;
using Unity.Attributes;

namespace PoeEye.PoeTrade.ViewModels
{
    internal sealed class PoeQueryViewModel : DisposableReactiveObject, IPoeQueryViewModel
    {
        private readonly ObservableCollectionExtended<IPoeCurrency> currencyList = new ObservableCollectionExtended<IPoeCurrency>();
        private readonly ObservableCollectionExtended<string> leagueList = new ObservableCollectionExtended<string>();

        private string accountName;
        private TriState? affectedByElderState;
        private TriState? affectedByShaperState;
        private bool alternativeArt;
        private float? apsMax;
        private float? apsMin;
        private float? armourMax;
        private float? armourMin;
        private float? blockMax;
        private float? blockMin;
        private string buyoutCurrencyType;
        private float? buyoutMax;
        private float? buyoutMin;
        private PoeBuyoutMode? buyoutMode;

        private bool captureFocusOnFirstGet = true;
        private TriState? corruptionState;
        private TriState? craftState;
        private float? critMax;
        private float? critMin;
        private float? damageMax;
        private float? damageMin;
        private float? dpsMax;
        private float? dpsMin;
        private float? edpsMax;
        private float? edpsMin;
        private TriState? enchantState;
        private float? evasionMax;
        private float? evasionMin;
        private int? incQuantityMax;
        private int? incQuantityMin;
        private bool isExpanded = true;
        private string itemBase;
        private int? itemLevelMax;
        private int? itemLevelMin;
        private string itemName;
        private PoeItemRarity? itemRarity;
        private string league;
        private int? levelMax;
        private int? levelMin;
        private int? linkedB;
        private int? linkedG;
        private int? linkedR;
        private int? linkedW;
        private int? linkMax;
        private int? linkMin;
        private bool normalizeQuality;
        private bool onlineOnly;
        private float? pdpsMax;
        private float? pdpsMin;
        private int? qualityMax;
        private int? qualityMin;
        private int? rDexMax;
        private int? rDexMin;
        private int? rIntMax;
        private int? rIntMin;
        private int? rLevelMax;
        private int? rLevelMin;
        private int? rStrMax;
        private int? rStrMin;
        private float? shieldMax;
        private float? shieldMin;
        private int? socketsB;
        private int? socketsG;
        private int? socketsMax;
        private int? socketsMin;
        private int? socketsR;
        private int? socketsW;

        public PoeQueryViewModel(
            [NotNull] IPoeStaticDataSource staticDataSource,
            [NotNull] IFactory<IPoeModGroupsEditorViewModel, IPoeStaticDataSource> modGroupsEditorFactory,
            [NotNull] IFactory<IPoeItemTypeSelectorViewModel, IPoeStaticDataSource> itemTypeSelectorFactory,
            [NotNull] IFactory<IReactiveSuggestionProvider> suggestionProviderFactory,
            [NotNull] IPoeDatabaseReader poeDatabaseReader,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(staticDataSource, nameof(staticDataSource));
            Guard.ArgumentNotNull(modGroupsEditorFactory, nameof(modGroupsEditorFactory));
            Guard.ArgumentNotNull(itemTypeSelectorFactory, nameof(itemTypeSelectorFactory));
            Guard.ArgumentNotNull(suggestionProviderFactory, nameof(suggestionProviderFactory));
            Guard.ArgumentNotNull(poeDatabaseReader, nameof(poeDatabaseReader));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));

            LeaguesList = new ReadOnlyObservableCollection<string>(leagueList);
            CurrenciesList = new ReadOnlyObservableCollection<IPoeCurrency>(currencyList);
            ItemTypeSelector = itemTypeSelectorFactory.Create(staticDataSource);
            ModGroupsEditor = modGroupsEditorFactory.Create(staticDataSource);

            OnlineOnly = true;
            BuyoutMode = PoeBuyoutMode.BuyoutOnly;
            NormalizeQuality = true;

            staticDataSource
                .WhenAnyValue(x => x.StaticData)
                .Subscribe(
                    staticData =>
                    {
                        leagueList.Clear();
                        currencyList.Clear();

                        leagueList.AddRange(staticData.LeaguesList);
                        currencyList.AddRange(staticData.CurrenciesList);
                    })
                .AddTo(Anchors);

            NameSuggestionProvider = suggestionProviderFactory.Create();
            NameSuggestionProvider.Items = poeDatabaseReader.KnownEntityNames;

            this.WhenAnyValue(x => x.League).Where(string.IsNullOrWhiteSpace).ToUnit()
                .Merge(leagueList.ToObservableChangeSet().ToUnit())
                .Where(x => string.IsNullOrWhiteSpace(League) && LeaguesList.Count > 0)
                .ObserveOn(uiScheduler)
                .Subscribe(() => League = LeaguesList.FirstOrDefault())
                .AddTo(Anchors);
        }

        public bool CaptureFocus
        {
            get
            {
                if (!captureFocusOnFirstGet)
                {
                    return false;
                }

                //FIXME This hack was implemented to focus on QueryTextBox when new tab is created. Usual approach is not working due to TabControl virtualisation
                captureFocusOnFirstGet = false;
                return true;
            }
        }

        public ReadOnlyObservableCollection<string> LeaguesList { get; }

        public ReadOnlyObservableCollection<IPoeCurrency> CurrenciesList { get; }

        public IPoeItemTypeSelectorViewModel ItemTypeSelector { get; }

        public IPoeModGroupsEditorViewModel ModGroupsEditor { get; }

        public IReactiveSuggestionProvider NameSuggestionProvider { get; }

        public string Description => GetQueryDescription();

        public Func<IPoeQueryInfo> PoeQueryBuilder => GetQueryInfo;

        public float? DamageMin
        {
            get => damageMin;
            set => this.RaiseAndSetIfChanged(ref damageMin, value);
        }

        public float? DamageMax
        {
            get => damageMax;
            set => this.RaiseAndSetIfChanged(ref damageMax, value);
        }

        public float? ApsMin
        {
            get => apsMin;
            set => this.RaiseAndSetIfChanged(ref apsMin, value);
        }

        public float? ApsMax
        {
            get => apsMax;
            set => this.RaiseAndSetIfChanged(ref apsMax, value);
        }

        public float? CritMin
        {
            get => critMin;
            set => this.RaiseAndSetIfChanged(ref critMin, value);
        }

        public float? CritMax
        {
            get => critMax;
            set => this.RaiseAndSetIfChanged(ref critMax, value);
        }

        public float? DpsMin
        {
            get => dpsMin;
            set => this.RaiseAndSetIfChanged(ref dpsMin, value);
        }

        public float? DpsMax
        {
            get => dpsMax;
            set => this.RaiseAndSetIfChanged(ref dpsMax, value);
        }

        public float? EdpsMin
        {
            get => edpsMin;
            set => this.RaiseAndSetIfChanged(ref edpsMin, value);
        }

        public float? EdpsMax
        {
            get => edpsMax;
            set => this.RaiseAndSetIfChanged(ref edpsMax, value);
        }

        public float? PdpsMin
        {
            get => pdpsMin;
            set => this.RaiseAndSetIfChanged(ref pdpsMin, value);
        }

        public float? PdpsMax
        {
            get => pdpsMax;
            set => this.RaiseAndSetIfChanged(ref pdpsMax, value);
        }

        public float? ArmourMin
        {
            get => armourMin;
            set => this.RaiseAndSetIfChanged(ref armourMin, value);
        }

        public float? ArmourMax
        {
            get => armourMax;
            set => this.RaiseAndSetIfChanged(ref armourMax, value);
        }

        public float? EvasionMin
        {
            get => evasionMin;
            set => this.RaiseAndSetIfChanged(ref evasionMin, value);
        }

        public float? EvasionMax
        {
            get => evasionMax;
            set => this.RaiseAndSetIfChanged(ref evasionMax, value);
        }

        public float? ShieldMin
        {
            get => shieldMin;
            set => this.RaiseAndSetIfChanged(ref shieldMin, value);
        }

        public float? ShieldMax
        {
            get => shieldMax;
            set => this.RaiseAndSetIfChanged(ref shieldMax, value);
        }

        public float? BlockMin
        {
            get => blockMin;
            set => this.RaiseAndSetIfChanged(ref blockMin, value);
        }

        public float? BlockMax
        {
            get => blockMax;
            set => this.RaiseAndSetIfChanged(ref blockMax, value);
        }

        public int? SocketsMin
        {
            get => socketsMin;
            set => this.RaiseAndSetIfChanged(ref socketsMin, value);
        }

        public int? SocketsMax
        {
            get => socketsMax;
            set => this.RaiseAndSetIfChanged(ref socketsMax, value);
        }

        public int? LinkMin
        {
            get => linkMin;
            set => this.RaiseAndSetIfChanged(ref linkMin, value);
        }

        public int? LinkMax
        {
            get => linkMax;
            set => this.RaiseAndSetIfChanged(ref linkMax, value);
        }

        public int? SocketsR
        {
            get => socketsR;
            set => this.RaiseAndSetIfChanged(ref socketsR, value);
        }

        public int? SocketsG
        {
            get => socketsG;
            set => this.RaiseAndSetIfChanged(ref socketsG, value);
        }

        public int? SocketsW
        {
            get => socketsW;
            set => this.RaiseAndSetIfChanged(ref socketsW, value);
        }

        public IPoeQueryModsGroup[] ModGroups => ModGroupsEditor.ToGroups();

        public int? SocketsB
        {
            get => socketsB;
            set => this.RaiseAndSetIfChanged(ref socketsB, value);
        }

        public int? RLevelMin
        {
            get => rLevelMin;
            set => this.RaiseAndSetIfChanged(ref rLevelMin, value);
        }

        public int? RLevelMax
        {
            get => rLevelMax;
            set => this.RaiseAndSetIfChanged(ref rLevelMax, value);
        }

        public int? RStrMin
        {
            get => rStrMin;
            set => this.RaiseAndSetIfChanged(ref rStrMin, value);
        }

        public int? RStrMax
        {
            get => rStrMax;
            set => this.RaiseAndSetIfChanged(ref rStrMax, value);
        }

        public int? RDexMin
        {
            get => rDexMin;
            set => this.RaiseAndSetIfChanged(ref rDexMin, value);
        }

        public int? RDexMax
        {
            get => rDexMax;
            set => this.RaiseAndSetIfChanged(ref rDexMax, value);
        }

        public int? RIntMin
        {
            get => rIntMin;
            set => this.RaiseAndSetIfChanged(ref rIntMin, value);
        }

        public int? RIntMax
        {
            get => rIntMax;
            set => this.RaiseAndSetIfChanged(ref rIntMax, value);
        }

        public int? QualityMin
        {
            get => qualityMin;
            set => this.RaiseAndSetIfChanged(ref qualityMin, value);
        }

        public int? QualityMax
        {
            get => qualityMax;
            set => this.RaiseAndSetIfChanged(ref qualityMax, value);
        }

        public int? LevelMin
        {
            get => levelMin;
            set => this.RaiseAndSetIfChanged(ref levelMin, value);
        }

        public int? LevelMax
        {
            get => levelMax;
            set => this.RaiseAndSetIfChanged(ref levelMax, value);
        }

        public int? ItemLevelMin
        {
            get => itemLevelMin;
            set => this.RaiseAndSetIfChanged(ref itemLevelMin, value);
        }

        public int? ItemLevelMax
        {
            get => itemLevelMax;
            set => this.RaiseAndSetIfChanged(ref itemLevelMax, value);
        }


        public int? IncQuantityMin
        {
            get => incQuantityMin;
            set => this.RaiseAndSetIfChanged(ref incQuantityMin, value);
        }

        public int? IncQuantityMax
        {
            get => incQuantityMax;
            set => this.RaiseAndSetIfChanged(ref incQuantityMax, value);
        }

        public float? BuyoutMin
        {
            get => buyoutMin;
            set => this.RaiseAndSetIfChanged(ref buyoutMin, value);
        }

        public float? BuyoutMax
        {
            get => buyoutMax;
            set => this.RaiseAndSetIfChanged(ref buyoutMax, value);
        }

        public string BuyoutCurrencyType
        {
            get => buyoutCurrencyType;
            set => this.RaiseAndSetIfChanged(ref buyoutCurrencyType, value);
        }

        public string League
        {
            get => league;
            set => this.RaiseAndSetIfChanged(ref league, value);
        }

        public string AccountName
        {
            get => accountName;
            set => this.RaiseAndSetIfChanged(ref accountName, value);
        }

        public bool OnlineOnly
        {
            get => onlineOnly;
            set => this.RaiseAndSetIfChanged(ref onlineOnly, value);
        }

        public PoeBuyoutMode? BuyoutMode
        {
            get => buyoutMode;
            set => this.RaiseAndSetIfChanged(ref buyoutMode, value);
        }

        public bool NormalizeQuality
        {
            get => normalizeQuality;
            set => this.RaiseAndSetIfChanged(ref normalizeQuality, value);
        }

        public bool AlternativeArt
        {
            get => alternativeArt;
            set => this.RaiseAndSetIfChanged(ref alternativeArt, value);
        }

        public string ItemName
        {
            get => itemName;
            set => this.RaiseAndSetIfChanged(ref itemName, value);
        }

        public string ItemBase
        {
            get => itemBase;
            set => this.RaiseAndSetIfChanged(ref itemBase, value);
        }

        public int? LinkedR
        {
            get => linkedR;
            set => this.RaiseAndSetIfChanged(ref linkedR, value);
        }

        public int? LinkedB
        {
            get => linkedB;
            set => this.RaiseAndSetIfChanged(ref linkedB, value);
        }

        public int? LinkedG
        {
            get => linkedG;
            set => this.RaiseAndSetIfChanged(ref linkedG, value);
        }

        public int? LinkedW
        {
            get => linkedW;
            set => this.RaiseAndSetIfChanged(ref linkedW, value);
        }

        public PoeItemRarity? ItemRarity
        {
            get => itemRarity;
            set => this.RaiseAndSetIfChanged(ref itemRarity, value);
        }

        public TriState? CorruptionState
        {
            get => corruptionState;
            set => this.RaiseAndSetIfChanged(ref corruptionState, value);
        }

        public TriState? CraftState
        {
            get => craftState;
            set => this.RaiseAndSetIfChanged(ref craftState, value);
        }

        public TriState? AffectedByElderState
        {
            get => affectedByElderState;
            set => this.RaiseAndSetIfChanged(ref affectedByElderState, value);
        }

        public TriState? AffectedByShaperState
        {
            get => affectedByShaperState;
            set => this.RaiseAndSetIfChanged(ref affectedByShaperState, value);
        }

        public TriState? EnchantState
        {
            get => enchantState;
            set => this.RaiseAndSetIfChanged(ref enchantState, value);
        }

        public bool IsExpanded
        {
            get => isExpanded;
            set => this.RaiseAndSetIfChanged(ref isExpanded, value);
        }

        public IPoeItemType ItemType => ItemTypeSelector.ToItemType();

        public void SetQueryInfo(IPoeQueryInfo source)
        {
            Guard.ArgumentNotNull(source, nameof(source));

            source.TransferPropertiesTo(this);

            ModGroupsEditor.Groups.Clear();
            foreach (var group in source.ModGroups.EmptyIfNull())
            {
                var newGroup = ModGroupsEditor.AddGroup();
                newGroup.Mods.Clear();
                newGroup.GroupType = group.GroupType;
                newGroup.MinGroupValue = group.Min;
                newGroup.MaxGroupValue = group.Max;

                foreach (var mod in group.Mods.EmptyIfNull().Where(x => !string.IsNullOrWhiteSpace(x.Name)))
                {
                    var newMod = newGroup.AddMod();
                    newMod.SelectedMod = mod.Name;
                    newMod.Max = mod.Max;
                    newMod.Min = mod.Min;
                }

                if (newGroup.Mods.Count == 0)
                {
                    newGroup.AddMod();
                }
            }

            if (ModGroupsEditor.Groups.Count == 0)
            {
                ModGroupsEditor.AddGroup();
            }

            if (source.ItemType != null)
            {
                ItemTypeSelector.SelectedValue = source.ItemType.Name;
            }

            this.RaisePropertyChanged(nameof(PoeQueryBuilder));
        }

        private IPoeQueryInfo GetQueryInfo()
        {
            var result = new PoeQueryInfo();

            ((IPoeQueryInfo)this).TransferPropertiesTo(result);

            return result;
        }

        private IList<string> FormatQueryDescriptionArray()
        {
            var blackList = new[]
            {
                nameof(League),
                nameof(ItemName)
            };
            var nullableProperties = typeof(IPoeQueryInfo)
                                     .GetProperties()
                                     .Where(x => !blackList.Contains(x.Name))
                                     .Where(
                                         x => x.PropertyType == typeof(int?)
                                              || x.PropertyType == typeof(float?)
                                              || x.PropertyType == typeof(string)
                                              || x.PropertyType == typeof(IPoeItemType)
                                              || x.PropertyType == typeof(PoeItemRarity?))
                                     .Where(x => x.CanRead)
                                     .ToArray();

            var result = new List<string>();
            foreach (var nullableProperty in nullableProperties)
            {
                var value = nullableProperty.GetValue(this);
                if (value == null)
                {
                    continue;
                }

                if (value is string s && string.IsNullOrWhiteSpace(s))
                {
                    continue;
                }

                var formattedValue = $"{nullableProperty.Name}: {value}";
                result.Add(formattedValue);
            }

            return result;
        }

        private string GetQueryDescription()
        {
            var descriptions = FormatQueryDescriptionArray();
            if (!string.IsNullOrEmpty(itemName))
            {
                descriptions.Insert(0, itemName);
            }

            if (!descriptions.Any())
            {
                return null;
            }

            var result = string.Join("\r\n", descriptions);
            return result;
        }
    }
}