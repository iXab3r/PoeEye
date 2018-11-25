﻿using PoeShared.Common;
using PoeShared.PoeTrade.Query;

namespace PoeShared.PoeTrade
{
    public interface IPoeQueryInfo
    {
        bool AlternativeArt { get; }

        float? ApsMax { get; }

        float? ApsMin { get; }

        float? ArmourMax { get; }

        float? ArmourMin { get; }

        float? BlockMax { get; }

        float? BlockMin { get; }

        string BuyoutCurrencyType { get; }

        float? BuyoutMax { get; }

        float? BuyoutMin { get; }

        float? CritMax { get; }

        float? CritMin { get; }

        float? DamageMax { get; }

        float? DamageMin { get; }

        float? DpsMax { get; }

        float? DpsMin { get; }

        float? EdpsMax { get; }

        float? EdpsMin { get; }

        float? EvasionMax { get; }

        float? EvasionMin { get; }

        int? IncQuantityMax { get; }

        int? IncQuantityMin { get; }

        string ItemBase { get; }

        string ItemName { get; }

        string AccountName { get; }

        PoeItemRarity? ItemRarity { get; }

        TriState? CorruptionState { get; }

        TriState? CraftState { get; }

        TriState? AffectedByShaperState { get; }

        TriState? AffectedByElderState { get; }

        TriState? EnchantState { get; }

        IPoeItemType ItemType { get; }

        PoeBuyoutMode? BuyoutMode { get; }

        string League { get; }

        int? LevelMax { get; }

        int? LevelMin { get; }

        int? ItemLevelMax { get; }

        int? ItemLevelMin { get; }

        int? LinkedB { get; }

        int? LinkedG { get; }

        int? LinkedR { get; }

        int? LinkedW { get; }

        int? LinkMax { get; }

        int? LinkMin { get; }

        bool NormalizeQuality { get; }

        bool OnlineOnly { get; }

        float? PdpsMax { get; }

        float? PdpsMin { get; }

        int? QualityMax { get; }

        int? QualityMin { get; }

        int? RDexMax { get; }

        int? RDexMin { get; }

        int? RIntMax { get; }

        int? RIntMin { get; }

        int? RLevelMax { get; }

        int? RLevelMin { get; }

        int? RStrMax { get; }

        int? RStrMin { get; }

        float? ShieldMax { get; }

        float? ShieldMin { get; }

        int? SocketsB { get; }

        int? SocketsG { get; }

        int? SocketsMax { get; }

        int? SocketsMin { get; }

        int? SocketsR { get; }

        int? SocketsW { get; }

        IPoeQueryModsGroup[] ModGroups { get; }
    }
}