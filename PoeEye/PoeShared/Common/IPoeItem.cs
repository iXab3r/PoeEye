﻿namespace PoeShared.Common
{
    using System;

    using JetBrains.Annotations;

    public interface IPoeItem
    {
        string ItemName { get; }

        string ItemIconUri { get; }

        string TradeForumUri { get; }

        string UserForumUri { get; }

        string UserForumName { get; }

        string UserIgn { get; }

        bool UserIsOnline { get; }

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

        string Level { get; }

        string Requirements { get; }

        string ThreadId { get; }

        string Hash { get; }

        string Note { get; }

        string SuggestedPrivateMessage { get; }

        DateTime? FirstSeen { get; }

        bool IsCorrupted { get; }

        bool IsUnidentified { get; }

        PoeItemRarity Rarity { get; }

        IPoeItemMod[] Mods { [NotNull] get; }

        IPoeLinksInfo Links { get; }

        DateTime Timestamp { get; set; }
    }
}