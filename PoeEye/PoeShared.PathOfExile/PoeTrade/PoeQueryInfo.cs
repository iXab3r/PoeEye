using System;
using System.Collections.Generic;
using PoeShared.Common;
using PoeShared.PoeTrade.Query;
using PoeShared.Scaffolding;

namespace PoeShared.PoeTrade
{
    public sealed class PoeQueryInfo : IPoeQueryInfo
    {
        public static readonly IPoeQueryInfo Empty = new PoeQueryInfo();

        public static readonly IEqualityComparer<IPoeQueryInfo> Comparer =
            new LambdaComparer<IPoeQueryInfo>((x, y) => string.CompareOrdinal(x?.DumpToText(), y?.DumpToText()) == 0);

        public static string Id { get; } = Guid.NewGuid().ToString();

        public string[] LeaguesList { get; set; }

        public IPoeQueryRangeModArgument[] Mods { get; set; }

        public bool IsExpanded { get; set; }

        public bool AlternativeArt { get; set; }

        public float? ApsMax { get; set; }

        public float? ApsMin { get; set; }

        public float? ArmourMax { get; set; }

        public float? ArmourMin { get; set; }

        public float? BlockMax { get; set; }

        public float? BlockMin { get; set; }

        public string BuyoutCurrencyType { get; set; }

        public float? BuyoutMax { get; set; }

        public float? BuyoutMin { get; set; }

        public PoeBuyoutMode? BuyoutMode { get; set; }

        public float? CritMax { get; set; }

        public float? CritMin { get; set; }

        public float? DamageMax { get; set; }

        public float? DamageMin { get; set; }

        public float? DpsMax { get; set; }

        public float? DpsMin { get; set; }

        public float? EdpsMax { get; set; }

        public float? EdpsMin { get; set; }

        public float? EvasionMax { get; set; }

        public float? EvasionMin { get; set; }

        public int? IncQuantityMax { get; set; }

        public int? IncQuantityMin { get; set; }

        public string ItemBase { get; set; }

        public string ItemName { get; set; }

        public string AccountName { get; set; }

        public PoeItemRarity? ItemRarity { get; set; }

        public TriState? CorruptionState { get; set; }

        public TriState? CraftState { get; set; }

        public TriState? AffectedByShaperState { get; set; }

        public TriState? AffectedByElderState { get; set; }

        public TriState? EnchantState { get; set; }

        public IPoeItemType ItemType { get; set; }

        public string League { get; set; }

        public int? LevelMax { get; set; }

        public int? LevelMin { get; set; }

        public int? ItemLevelMax { get; set; }

        public int? ItemLevelMin { get; set; }

        public int? LinkedB { get; set; }

        public int? LinkedG { get; set; }

        public int? LinkedR { get; set; }

        public int? LinkedW { get; set; }

        public int? LinkMax { get; set; }

        public int? LinkMin { get; set; }

        public bool NormalizeQuality { get; set; }

        public bool OnlineOnly { get; set; }

        public float? PdpsMax { get; set; }

        public float? PdpsMin { get; set; }

        public int? QualityMax { get; set; }

        public int? QualityMin { get; set; }

        public int? RDexMax { get; set; }

        public int? RDexMin { get; set; }

        public int? RIntMax { get; set; }

        public int? RIntMin { get; set; }

        public int? RLevelMax { get; set; }

        public int? RLevelMin { get; set; }

        public int? RStrMax { get; set; }

        public int? RStrMin { get; set; }

        public float? ShieldMax { get; set; }

        public float? ShieldMin { get; set; }

        public int? SocketsB { get; set; }

        public int? SocketsG { get; set; }

        public int? SocketsMax { get; set; }

        public int? SocketsMin { get; set; }

        public int? SocketsR { get; set; }

        public int? SocketsW { get; set; }

        public IPoeQueryModsGroup[] ModGroups { get; set; }
    }
}