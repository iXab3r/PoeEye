namespace PoeShared.Common
{
    using System;

    public sealed class PoeItem : IPoeItem
    {
        private IPoeItemMod[] mods = new IPoeItemMod[0];

        public string ItemName { get; set; }

        public string ItemIconUri { get; set; }

        public string TradeForumUri { get; set; }

        public string UserForumUri { get; set; }

        public string UserForumName { get; set; }

        public string UserIgn { get; set; }

        public bool UserIsOnline { get; set; }

        public string Price { get; set; }

        public string League { get; set; }

        public string Quality { get; set; }

        public string Physical { get; set; }

        public string Elemental { get; set; }

        public string AttacksPerSecond { get; set; }

        public string DamagePerSecond { get; set; }

        public string PhysicalDamagePerSecond { get; set; }

        public string ElementalDamagePerSecond { get; set; }

        public string Armour { get; set; }

        public string Evasion { get; set; }

        public string Shield { get; set; }

        public string BlockChance { get; set; }

        public string CriticalChance { get; set; }

        public string Level { get; set; }

        public string Requirements { get; set; }

        public string ThreadId { get; set; }

        public string Hash { get; set; }

        public string Note { get; set; }

        public string SuggestedPrivateMessage { get; set; }

        public DateTime? FirstSeen { get; set; }

        public bool IsCorrupted { get; set; }

        public bool IsMirrored { get; set; }

        public bool IsUnidentified { get; set; }

        public PoeItemRarity Rarity { get; set; }

        public string Raw { get; set; }

        public IPoeItemMod[] Mods
        {
            get { return mods; }
            set { mods = value ?? new IPoeItemMod[0]; }
        }

        public IPoeLinksInfo Links { get; set; }

        public DateTime Timestamp { get; set; }
    }
}