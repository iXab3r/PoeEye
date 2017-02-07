using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Guards;
using JetBrains.Annotations;
using PoeShared.Common;
using PoeShared.PoeDatabase;
using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;
using WpfAutoCompleteControls.Editors;

namespace PoeEye.PoeTrade.ViewModels
{
    internal sealed class PoeQueryViewModel : DisposableReactiveObject, IPoeQueryViewModel
    {
        private string accountName;
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

        private PoeItemCorruptionState? corruptionState;

        private float? critMax;

        private float? critMin;

        private float? damageMax;

        private float? damageMin;

        private float? dpsMax;

        private float? dpsMin;

        private float? edpsMax;

        private float? edpsMin;

        private float? evasionMax;

        private float? evasionMin;

        private int? gemOrMapLevelMax;

        private int? gemOrMapLevelMin;

        private int? incQuantityMax;

        private int? incQuantityMin;

        private bool isExpanded = true;

        private string itemBase;

        private string itemName;

        private PoeItemRarity? itemRarity;

        private IPoeItemType itemType;

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
            [NotNull] IPoeStaticData staticData,
            [NotNull] IFactory<IPoeModGroupsEditorViewModel, IPoeStaticData> modGroupsEditorFactory,
            [NotNull] IFactory<ISuggestionProvider, string[]> suggestionProviderFactory,
            [NotNull] IPoeDatabaseReader poeDatabaseReader)
        {
            Guard.ArgumentNotNull(() => staticData);
            Guard.ArgumentNotNull(() => modGroupsEditorFactory);
            Guard.ArgumentNotNull(() => suggestionProviderFactory);
            Guard.ArgumentNotNull(() => poeDatabaseReader);

            LeaguesList = staticData.LeaguesList.ToArray();

            CurrenciesList = staticData.CurrenciesList.ToArray();

            ItemTypes = staticData.ItemTypes.ToArray();

            ModGroupsEditor = modGroupsEditorFactory.Create(staticData);

            OnlineOnly = true;
            BuyoutMode = PoeBuyoutMode.BuyoutOnly;
            NormalizeQuality = true;

            this.WhenAnyValue(x => x.League)
                .Where(x => string.IsNullOrWhiteSpace(x) || !LeaguesList.Contains(x, StringComparer.Ordinal))
                .Subscribe(() => League = LeaguesList.FirstOrDefault())
                .AddTo(Anchors);

            var knownNames = poeDatabaseReader.KnownEntitiesNames;
            NameSuggestionProvider = suggestionProviderFactory.Create(knownNames);
        }

        public int? GemOrMapLevelMin
        {
            get { return gemOrMapLevelMin; }
            set { this.RaiseAndSetIfChanged(ref gemOrMapLevelMin, value); }
        }

        public int? GemOrMapLevelMax
        {
            get { return gemOrMapLevelMax; }
            set { this.RaiseAndSetIfChanged(ref gemOrMapLevelMax, value); }
        }

        public string[] LeaguesList { get; }

        public IPoeCurrency[] CurrenciesList { get; }

        public IPoeModGroupsEditorViewModel ModGroupsEditor { get; }

        public ISuggestionProvider NameSuggestionProvider { get; }

        public IPoeItemType[] ItemTypes { get; }

        public string Description => GetQueryDescription();

        public Func<IPoeQueryInfo> PoeQueryBuilder => GetQueryInfo;

        public float? DamageMin
        {
            get { return damageMin; }
            set { this.RaiseAndSetIfChanged(ref damageMin, value); }
        }

        public float? DamageMax
        {
            get { return damageMax; }
            set { this.RaiseAndSetIfChanged(ref damageMax, value); }
        }

        public float? ApsMin
        {
            get { return apsMin; }
            set { this.RaiseAndSetIfChanged(ref apsMin, value); }
        }

        public float? ApsMax
        {
            get { return apsMax; }
            set { this.RaiseAndSetIfChanged(ref apsMax, value); }
        }

        public float? CritMin
        {
            get { return critMin; }
            set { this.RaiseAndSetIfChanged(ref critMin, value); }
        }

        public float? CritMax
        {
            get { return critMax; }
            set { this.RaiseAndSetIfChanged(ref critMax, value); }
        }

        public float? DpsMin
        {
            get { return dpsMin; }
            set { this.RaiseAndSetIfChanged(ref dpsMin, value); }
        }

        public float? DpsMax
        {
            get { return dpsMax; }
            set { this.RaiseAndSetIfChanged(ref dpsMax, value); }
        }

        public float? EdpsMin
        {
            get { return edpsMin; }
            set { this.RaiseAndSetIfChanged(ref edpsMin, value); }
        }

        public float? EdpsMax
        {
            get { return edpsMax; }
            set { this.RaiseAndSetIfChanged(ref edpsMax, value); }
        }

        public float? PdpsMin
        {
            get { return pdpsMin; }
            set { this.RaiseAndSetIfChanged(ref pdpsMin, value); }
        }

        public float? PdpsMax
        {
            get { return pdpsMax; }
            set { this.RaiseAndSetIfChanged(ref pdpsMax, value); }
        }

        public float? ArmourMin
        {
            get { return armourMin; }
            set { this.RaiseAndSetIfChanged(ref armourMin, value); }
        }

        public float? ArmourMax
        {
            get { return armourMax; }
            set { this.RaiseAndSetIfChanged(ref armourMax, value); }
        }

        public float? EvasionMin
        {
            get { return evasionMin; }
            set { this.RaiseAndSetIfChanged(ref evasionMin, value); }
        }

        public float? EvasionMax
        {
            get { return evasionMax; }
            set { this.RaiseAndSetIfChanged(ref evasionMax, value); }
        }

        public float? ShieldMin
        {
            get { return shieldMin; }
            set { this.RaiseAndSetIfChanged(ref shieldMin, value); }
        }

        public float? ShieldMax
        {
            get { return shieldMax; }
            set { this.RaiseAndSetIfChanged(ref shieldMax, value); }
        }

        public float? BlockMin
        {
            get { return blockMin; }
            set { this.RaiseAndSetIfChanged(ref blockMin, value); }
        }

        public float? BlockMax
        {
            get { return blockMax; }
            set { this.RaiseAndSetIfChanged(ref blockMax, value); }
        }

        public int? SocketsMin
        {
            get { return socketsMin; }
            set { this.RaiseAndSetIfChanged(ref socketsMin, value); }
        }

        public int? SocketsMax
        {
            get { return socketsMax; }
            set { this.RaiseAndSetIfChanged(ref socketsMax, value); }
        }

        public int? LinkMin
        {
            get { return linkMin; }
            set { this.RaiseAndSetIfChanged(ref linkMin, value); }
        }

        public int? LinkMax
        {
            get { return linkMax; }
            set { this.RaiseAndSetIfChanged(ref linkMax, value); }
        }

        public int? SocketsR
        {
            get { return socketsR; }
            set { this.RaiseAndSetIfChanged(ref socketsR, value); }
        }

        public int? SocketsG
        {
            get { return socketsG; }
            set { this.RaiseAndSetIfChanged(ref socketsG, value); }
        }

        public int? SocketsW
        {
            get { return socketsW; }
            set { this.RaiseAndSetIfChanged(ref socketsW, value); }
        }

        public IPoeQueryModsGroup[] ModGroups => ModGroupsEditor.ToGroups();

        public int? SocketsB
        {
            get { return socketsB; }
            set { this.RaiseAndSetIfChanged(ref socketsB, value); }
        }

        public int? RLevelMin
        {
            get { return rLevelMin; }
            set { this.RaiseAndSetIfChanged(ref rLevelMin, value); }
        }

        public int? RLevelMax
        {
            get { return rLevelMax; }
            set { this.RaiseAndSetIfChanged(ref rLevelMax, value); }
        }

        public int? RStrMin
        {
            get { return rStrMin; }
            set { this.RaiseAndSetIfChanged(ref rStrMin, value); }
        }

        public int? RStrMax
        {
            get { return rStrMax; }
            set { this.RaiseAndSetIfChanged(ref rStrMax, value); }
        }

        public int? RDexMin
        {
            get { return rDexMin; }
            set { this.RaiseAndSetIfChanged(ref rDexMin, value); }
        }

        public int? RDexMax
        {
            get { return rDexMax; }
            set { this.RaiseAndSetIfChanged(ref rDexMax, value); }
        }

        public int? RIntMin
        {
            get { return rIntMin; }
            set { this.RaiseAndSetIfChanged(ref rIntMin, value); }
        }

        public int? RIntMax
        {
            get { return rIntMax; }
            set { this.RaiseAndSetIfChanged(ref rIntMax, value); }
        }

        public int? QualityMin
        {
            get { return qualityMin; }
            set { this.RaiseAndSetIfChanged(ref qualityMin, value); }
        }

        public int? QualityMax
        {
            get { return qualityMax; }
            set { this.RaiseAndSetIfChanged(ref qualityMax, value); }
        }

        public int? LevelMin
        {
            get { return levelMin; }
            set { this.RaiseAndSetIfChanged(ref levelMin, value); }
        }

        public int? LevelMax
        {
            get { return levelMax; }
            set { this.RaiseAndSetIfChanged(ref levelMax, value); }
        }

        public int? IncQuantityMin
        {
            get { return incQuantityMin; }
            set { this.RaiseAndSetIfChanged(ref incQuantityMin, value); }
        }

        public int? IncQuantityMax
        {
            get { return incQuantityMax; }
            set { this.RaiseAndSetIfChanged(ref incQuantityMax, value); }
        }

        public float? BuyoutMin
        {
            get { return buyoutMin; }
            set { this.RaiseAndSetIfChanged(ref buyoutMin, value); }
        }

        public float? BuyoutMax
        {
            get { return buyoutMax; }
            set { this.RaiseAndSetIfChanged(ref buyoutMax, value); }
        }

        public string BuyoutCurrencyType
        {
            get { return buyoutCurrencyType; }
            set { this.RaiseAndSetIfChanged(ref buyoutCurrencyType, value); }
        }

        public string League
        {
            get { return league; }
            set { this.RaiseAndSetIfChanged(ref league, value); }
        }

        public string AccountName
        {
            get { return accountName; }
            set { this.RaiseAndSetIfChanged(ref accountName, value); }
        }

        public bool OnlineOnly
        {
            get { return onlineOnly; }
            set { this.RaiseAndSetIfChanged(ref onlineOnly, value); }
        }

        public PoeBuyoutMode? BuyoutMode
        {
            get { return buyoutMode; }
            set { this.RaiseAndSetIfChanged(ref buyoutMode, value); }
        }

        public bool NormalizeQuality
        {
            get { return normalizeQuality; }
            set { this.RaiseAndSetIfChanged(ref normalizeQuality, value); }
        }

        public bool AlternativeArt
        {
            get { return alternativeArt; }
            set { this.RaiseAndSetIfChanged(ref alternativeArt, value); }
        }

        public string ItemName
        {
            get { return itemName; }
            set { this.RaiseAndSetIfChanged(ref itemName, value); }
        }

        public string ItemBase
        {
            get { return itemBase; }
            set { this.RaiseAndSetIfChanged(ref itemBase, value); }
        }

        public int? LinkedR
        {
            get { return linkedR; }
            set { this.RaiseAndSetIfChanged(ref linkedR, value); }
        }

        public int? LinkedB
        {
            get { return linkedB; }
            set { this.RaiseAndSetIfChanged(ref linkedB, value); }
        }

        public int? LinkedG
        {
            get { return linkedG; }
            set { this.RaiseAndSetIfChanged(ref linkedG, value); }
        }

        public int? LinkedW
        {
            get { return linkedW; }
            set { this.RaiseAndSetIfChanged(ref linkedW, value); }
        }

        public PoeItemRarity? ItemRarity
        {
            get { return itemRarity; }
            set { this.RaiseAndSetIfChanged(ref itemRarity, value); }
        }

        public PoeItemCorruptionState? CorruptionState
        {
            get { return corruptionState; }
            set { this.RaiseAndSetIfChanged(ref corruptionState, value); }
        }

        public bool IsExpanded
        {
            get { return isExpanded; }
            set { this.RaiseAndSetIfChanged(ref isExpanded, value); }
        }

        public IPoeItemType ItemType
        {
            get { return itemType; }
            set { this.RaiseAndSetIfChanged(ref itemType, value); }
        }

        public IPoeQueryInfo GetQueryInfo()
        {
            var result = new PoeQueryInfo();

            ((IPoeQueryInfo) this).TransferPropertiesTo(result);

            return result;
        }

        public void SetQueryInfo(IPoeQueryInfo source)
        {
            Guard.ArgumentNotNull(() => source);

            source.TransferPropertiesTo(this);

            if (source.ModGroups != null && source.ModGroups.Any())
            {
                ModGroupsEditor.Groups.Clear();
                foreach (var group in source.ModGroups.Where(x => x.Mods != null && x.Mods.Any()))
                {
                    var newGroup = ModGroupsEditor.AddGroup();
                    newGroup.Mods.Clear();
                    newGroup.GroupType = group.GroupType;
                    newGroup.MinGroupValue = group.Min;
                    newGroup.MaxGroupValue = group.Max;

                    foreach (var mod in group.Mods.Where(x => !string.IsNullOrWhiteSpace(x.Name)))
                    {
                        var newMod = newGroup.AddMod();
                        newMod.SelectedMod = mod.Name;
                        newMod.Max = mod.Max;
                        newMod.Min = mod.Min;
                    }
                }
            }

            if (source.ItemType != null)
            {
                var mappedItemType = ItemTypes.FirstOrDefault(x => x.CodeName == source.ItemType.CodeName);
                if (mappedItemType != null)
                {
                    ItemType = mappedItemType;
                }
            }

            this.RaisePropertyChanged(nameof(League));
            this.RaisePropertyChanged(nameof(PoeQueryBuilder));
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
                if (value is string && string.IsNullOrWhiteSpace(value as string))
                {
                    continue;
                }

                var formattedValue = $"{nullableProperty.Name}: {value}";
                result.Add(formattedValue);
            }
            return result;
        }

        public string GetQueryDescription()
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