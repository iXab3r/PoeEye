namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using DumpToText;

    using Factory;

    using Guards;

    using JetBrains.Annotations;

    using Models;

    using PoeShared;
    using PoeShared.Common;
    using PoeShared.PoeDatabase;
    using PoeShared.PoeTrade;
    using PoeShared.PoeTrade.Query;

    using ReactiveUI;

    using WpfAutoCompleteControls.Editors;

    internal sealed class PoeQueryViewModel : ReactiveObject, IPoeQueryInfo
    {
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

        private bool buyoutOnly;

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

        private PoeItemRarity? itemRarity;

        public PoeQueryViewModel(
            [NotNull] IPoeQueryInfoProvider queryInfoProvider,
            [NotNull] PoeImplicitModViewModel poeImplicitModViewModel,
            [NotNull] PoeExplicitModsEditorViewModel poeExplicitModsEditorViewModel,
            [NotNull] IFactory<GenericSuggestionProvider, string[]> suggestionProviderFactory,
            [NotNull] IPoeDatabaseReader poeDatabaseReader)
        {
            Guard.ArgumentNotNull(() => queryInfoProvider);
            Guard.ArgumentNotNull(() => poeImplicitModViewModel);
            Guard.ArgumentNotNull(() => poeExplicitModsEditorViewModel);
            Guard.ArgumentNotNull(() => suggestionProviderFactory);
            Guard.ArgumentNotNull(() => poeDatabaseReader);

            LeaguesList = queryInfoProvider.LeaguesList.ToArray();

            CurrenciesList = queryInfoProvider.CurrenciesList.ToArray();

            ItemTypes = queryInfoProvider.ItemTypes.ToArray();

            ImplicitModViewModel = poeImplicitModViewModel;
            ExplicitModsEditorViewModel = poeExplicitModsEditorViewModel;

            OnlineOnly = true;
            BuyoutOnly = true;
            NormalizeQuality = true;
            League = LeaguesList.First();

            var knownNames = poeDatabaseReader.KnownEntitiesNames;
            NameSuggestionProvider = suggestionProviderFactory.Create(knownNames);
        }

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

        IPoeQueryRangeModArgument IPoeQueryInfo.ImplicitMod => GetImplicitMod();

        IPoeQueryRangeModArgument[] IPoeQueryInfo.ExplicitMods => GetExplicitMods();

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

        public bool OnlineOnly
        {
            get { return onlineOnly; }
            set { this.RaiseAndSetIfChanged(ref onlineOnly, value); }
        }

        public bool BuyoutOnly
        {
            get { return buyoutOnly; }
            set { this.RaiseAndSetIfChanged(ref buyoutOnly, value); }
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

        public bool IsExpanded
        {
            get { return isExpanded; }
            set { this.RaiseAndSetIfChanged(ref isExpanded, value); }
        }

        public string[] LeaguesList { get; }

        public IPoeCurrency[] CurrenciesList { get; }

        public PoeImplicitModViewModel ImplicitModViewModel { get; }

        public PoeExplicitModsEditorViewModel ExplicitModsEditorViewModel { get; }

        public ISuggestionProvider NameSuggestionProvider { get; }

        public IPoeItemType[] ItemTypes { get; }

        public Func<IPoeQueryInfo> PoeQueryBuilder => GetQueryInfo;

        public IPoeItemType ItemType
        {
            get { return itemType; }
            set { this.RaiseAndSetIfChanged(ref itemType, value); }
        }

        private IPoeQueryInfo GetQueryInfo()
        {
            var result = new PoeQueryInfo();

            TransferProperties((IPoeQueryInfo)this, result);

            return result;
        }

        public void SetQueryInfo([NotNull] IPoeQueryInfo source)
        {
            Guard.ArgumentNotNull(() => source);

            TransferProperties(source, this);

            if (source.ImplicitMod != null)
            {
                ImplicitModViewModel.Min = source.ImplicitMod.Min;
                ImplicitModViewModel.Max = source.ImplicitMod.Max;
                ImplicitModViewModel.SelectedMod = source.ImplicitMod.Name;
            }

            if (source.ExplicitMods != null && source.ExplicitMods.Any())
            {
                ExplicitModsEditorViewModel.ClearMods();
                foreach (var mod in source.ExplicitMods.Where(x => !string.IsNullOrWhiteSpace(x.Name)))
                {
                    var newMod = ExplicitModsEditorViewModel.AddMod();
                    newMod.SelectedMod = mod.Name;
                    newMod.Max = mod.Max;
                    newMod.Min = mod.Min;
                    newMod.Excluded = mod.Excluded;
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
        }

        private static void TransferProperties<TSource, TTarget>(TSource source, TTarget target)
            where TTarget : class, TSource
        {
            var settableProperties = typeof(TTarget)
              .GetProperties(BindingFlags.Instance | BindingFlags.Public)
              .Where(x => x.CanRead && x.CanWrite)
              .ToArray();

            var propertiesToSet = typeof(TSource)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.CanRead)
                .ToArray();

            var skippedProperties = new List<PropertyInfo>();
            foreach (var property in propertiesToSet)
            {
                try
                {
                    var currentValue = property.GetValue(source);

                    var settableProperty = settableProperties.FirstOrDefault(x => x.Name == property.Name);
                    if (settableProperty == null)
                    {
                        skippedProperties.Add(property);
                        continue;
                    }
                    settableProperty.SetValue(target, currentValue);
                }
                catch (Exception ex)
                {
                    throw new ApplicationException(
                        $"Exception occurred, property: {property}\r\n" +
                        $"Settable properties: {settableProperties.Select(x => $"{x.PropertyType} {x.Name}").DumpToTextValue()}\r\n" +
                        $"PropertiesToSet: {propertiesToSet.Select(x => $"{x.PropertyType} {x.Name}").DumpToTextValue()}",
                        ex);
                }
            }
            if (skippedProperties.Any())
            {
                Log.Instance.Debug($"[TransferProperties] Skipped following properties:\r\n{skippedProperties.Select(x => $"{x.PropertyType} {x.Name}").DumpToTextValue()}");
            }
        }

        private IPoeQueryRangeModArgument GetImplicitMod()
        {
            if (string.IsNullOrWhiteSpace(ImplicitModViewModel.SelectedMod))
            {
                return null;
            }
            return new PoeQueryRangeModArgument(ImplicitModViewModel.SelectedMod)
            {
                Min = ImplicitModViewModel.Min,
                Max = ImplicitModViewModel.Max,
            };
        }

        private IPoeQueryRangeModArgument[] GetExplicitMods()
        {
            var result = new List<IPoeQueryRangeModArgument>();
            foreach (var poeExplicitModViewModel in ExplicitModsEditorViewModel.Mods)
            {
                if (string.IsNullOrWhiteSpace(poeExplicitModViewModel.SelectedMod))
                {
                    continue;
                }

                var explicitMod = new PoeQueryRangeModArgument(poeExplicitModViewModel.SelectedMod)
                {
                    Excluded = poeExplicitModViewModel.Excluded,
                    Min = poeExplicitModViewModel.Min,
                    Max = poeExplicitModViewModel.Max,
                };

                result.Add(explicitMod);
            }
            return result.ToArray();
        }

        public string FormatQueryDescription()
        {
            return GetQueryInfo().ToString();
        }
    }
}