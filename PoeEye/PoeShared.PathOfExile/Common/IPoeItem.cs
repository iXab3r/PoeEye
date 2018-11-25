﻿using System;
using System.Windows;
using JetBrains.Annotations;
using PoeShared.StashApi.ProcurementLegacy;

namespace PoeShared.Common
{
    public interface IPoeItem : IEquatable<IPoeItem>
    {
        string ItemName { get; }

        ItemTypeInfo TypeInfo { get; }

        string FlavourText { get; }

        string ItemIconUri { get; }

        string TradeForumUri { get; }

        string UserForumUri { get; }

        string UserForumName { get; }

        string UserIgn { get; }

        string Price { get; }

        string League { get; }

        string Quality { get; }

        string Physical { get; }

        string Elemental { get; }

        string AttacksPerSecond { get; }

        string DamagePerSecond { get; }

        string PhysicalDamagePerSecond { get; }

        string ElementalDamagePerSecond { get; }

        string Armour { get; }

        string Evasion { get; }

        string Shield { get; }

        string BlockChance { get; }

        string CriticalChance { get; }

        string ItemLevel { get; }

        string Requirements { get; }

        string ThreadId { get; }

        string Hash { get; }

        string Note { get; }

        string Raw { get; }

        string TabName { get; }

        Point? PositionInsideTab { get; }

        string SuggestedPrivateMessage { get; }

        DateTime? FirstSeen { get; }

        bool UserIsOnline { get; }

        PoeItemModificatins Modifications { get; }

        PoeTradeState ItemState { get; set; }

        PoeItemRarity Rarity { get; }

        IPoeItemMod[] Mods { [NotNull] get; }

        IPoeLinksInfo Links { get; }

        DateTime? Timestamp { get; set; }
    }
}