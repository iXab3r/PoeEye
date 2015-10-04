namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Factory;

    using Guards;

    using JetBrains.Annotations;

    using Models;

    using PoeShared.Common;
    using PoeShared.PoeDatabase;
    using PoeShared.PoeTrade.Query;

    using ReactiveUI;

    using WpfControls;
    using WpfControls.Editors;

    internal sealed class PoeQueryViewModel : ReactiveObject
    {
        private const string AnyKey = "any";
        public static IPoeItemType AnyItemType = new PoeItemType {Name = AnyKey};
        private readonly PoeExplicitModsEditorViewModel poeExplicitModsEditorViewModel;
        private readonly IPoeQueryInfoProvider queryInfoProvider;

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

            this.queryInfoProvider = queryInfoProvider;
            this.poeExplicitModsEditorViewModel = poeExplicitModsEditorViewModel;

            LeaguesList = queryInfoProvider.LeaguesList.ToArray();

            CurrenciesList = queryInfoProvider.CurrenciesList.ToArray();

            ItemTypes = queryInfoProvider.ItemTypes.ToArray();

            ImplicitModViewModel = poeImplicitModViewModel;
            ExplicitModsEditorViewModel = poeExplicitModsEditorViewModel;

            League = LeaguesList.First();

            OnlineOnly = true;
            BuyoutOnly = true;
            NormalizeQuality = true;

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

        public Func<IPoeQuery> PoeQueryBuilder => ConstructQuery;

        public IPoeItemType ItemType
        {
            get { return itemType; }
            set { this.RaiseAndSetIfChanged(ref itemType, value); }
        }

        private IPoeQuery ConstructQuery()
        {
            var result = new PoeQuery();

            var args = new List<IPoeQueryArgument>
            {
                CreateArgument("dmg_min", damageMax),
                CreateArgument("dmg_max", damageMax),
                CreateArgument("aps_min", apsMin),
                CreateArgument("aps_max", apsMax),
                CreateArgument("crit_min", critMin),
                CreateArgument("crit_max", critMax),
                CreateArgument("dps_min", dpsMin),
                CreateArgument("dps_max", dpsMax),
                CreateArgument("edps_min", edpsMin),
                CreateArgument("edps_max", edpsMax),
                CreateArgument("pdps_min", pdpsMin),
                CreateArgument("pdps_max", pdpsMax),
                CreateArgument("armour_min", armourMin),
                CreateArgument("armour_max", armourMax),
                CreateArgument("evasion_min", evasionMin),
                CreateArgument("evasion_max", evasionMax),
                CreateArgument("shield_min", shieldMin),
                CreateArgument("shield_max", shieldMax),
                CreateArgument("block_min", blockMin),
                CreateArgument("block_max", blockMax),
                CreateArgument("sockets_min", socketsMin),
                CreateArgument("sockets_max", socketsMax),
                CreateArgument("link_min", linkMin),
                CreateArgument("link_max", linkMin),
                CreateArgument("rlevel_min", rLevelMin),
                CreateArgument("rlevel_max", rLevelMax),
                CreateArgument("rstr_min", rStrMin),
                CreateArgument("rstr_max", rStrMax),
                CreateArgument("rdex_min", rDexMin),
                CreateArgument("rdex_max", rDexMax),
                CreateArgument("rint_min", rIntMin),
                CreateArgument("rint_max", rIntMax),
                CreateArgument("q_min", qualityMin),
                CreateArgument("q_max", qualityMax),
                CreateArgument("level_min", levelMin),
                CreateArgument("level_max", levelMax),
                CreateArgument("mapq_min", GemOrMapLevelMin),
                CreateArgument("mapq_max", GemOrMapLevelMax),
                CreateArgument("buyout_min", buyoutMin),
                CreateArgument("buyout_max", buyoutMax),
                CreateArgument("buyout", buyoutOnly),
                CreateArgument("online", onlineOnly),
                CreateArgument("altart", alternativeArt),
                CreateArgument("capquality", normalizeQuality),
                CreateArgument("base", itemBase),
                CreateArgument("name", itemName),
                CreateArgument("league", league),
                CreateArgument("sockets_r", socketsR),
                CreateArgument("sockets_g", socketsG),
                CreateArgument("sockets_b", socketsB),
                CreateArgument("sockets_w", socketsW),
                CreateArgument("linked_r", linkedR),
                CreateArgument("linked_g", linkedG),
                CreateArgument("linked_b", linkedB),
                CreateArgument("linked_w", linkedW),
                CreateArgument("type", itemType?.CodeName),
                CreateArgument("rarity", itemRarity?.ToString() ?? string.Empty),
            };

            if (ImplicitModViewModel.SelectedMod != null)
            {
                args.AddRange(new[]
                {
                    CreateArgument("impl", ImplicitModViewModel.SelectedMod),
                    CreateArgument("impl_min", ImplicitModViewModel.Min),
                    CreateArgument("impl_max", ImplicitModViewModel.Max)
                });
            }

            args.AddRange(new[]
            {
                CreateArgument("mods", string.Empty),
                CreateArgument("modexclude", string.Empty),
                CreateArgument("modmin", string.Empty),
                CreateArgument("modmax", string.Empty)
            });

            foreach (var poeExplicitModViewModel in ExplicitModsEditorViewModel.Mods.Where(x => x.SelectedMod != null))
            {
                var modArg = CreateModArgument(
                    poeExplicitModViewModel.SelectedMod,
                    poeExplicitModViewModel.Min,
                    poeExplicitModViewModel.Max);
                args.Add(modArg);
            }

            Guard.ArgumentIsTrue(() => args.ToDictionary(x => x.Name, x => default(int?)).Count() == args.Count());

            result.Arguments = args.ToArray();
            return result;
        }

        private IPoeQueryArgument CreateModArgument(string modName, float? min, float? max)
        {
            var arg = new PoeQueryRangeModArgument(modName)
            {
                Min = min,
                Max = max
            };
            return arg;
        }

        private IPoeQueryArgument CreateModArgument(IPoeItemMod mod, float? min, float? max)
        {
            return CreateModArgument(mod.CodeName, min, max);
        }

        private IPoeQueryArgument CreateArgument<T>(string name, T value)
        {
            if (value == null || Equals(value, default(T)))
            {
                return new PoeQueryStringArgument(name, string.Empty);
            }

            if (typeof (T) == typeof (int?))
            {
                return new PoeQueryIntArgument(name,
                    value is int ? ConvertToType<int>(value) : (int) ConvertToType<int?>(value));
            }
            if (typeof (T) == typeof (float?))
            {
                return new PoeQueryFloatArgument(name,
                    value is float ? ConvertToType<float>(value) : (float) ConvertToType<float?>(value));
            }
            if (typeof (T) == typeof (string))
            {
                return new PoeQueryStringArgument(name, ConvertToType<string>(value) ?? string.Empty);
            }
            if (typeof (T) == typeof (bool))
            {
                return new PoeQueryStringArgument(name, ConvertToType<bool>(value) ? "x" : string.Empty);
            }
            throw new NotSupportedException($"Type {typeof (T)} is not supported, parameter name: {name}");
        }

        private T ConvertToType<T>(object value)
        {
            return (T) Convert.ChangeType(value, typeof (T));
        }

        private string[] FormatQueryDescriptionArray()
        {
            var blackList = new[]
            {
                nameof(League),
            };
            var nullableProperties = typeof (PoeQueryViewModel)
                .GetProperties()
                .Where(x => !blackList.Contains(x.Name))
                .Where(x => x.PropertyType == typeof (int?)
                            || x.PropertyType == typeof (float?)
                            || x.PropertyType == typeof (string)
                            || x.PropertyType == typeof (IPoeItemType)
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
            return result.ToArray();
        }

        public string FormatQueryDescription()
        {
            var descriptions = FormatQueryDescriptionArray();
            if (!descriptions.Any())
            {
                return null;
            }
            return String.Join("\r\n", descriptions);
        }
    }
}