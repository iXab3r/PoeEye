using System;
using System.Collections.Generic;
using Nest;
using Newtonsoft.Json;

namespace PoeEye.ExileToolsApi.Entities
{
    [ElasticsearchType(Name = "item")]
    public class ExTzItem
    {
        [JsonProperty("info")]
        public ExTzItemInfo Info { get; set; }

        [JsonProperty("shop")]
        public ExTzShopInfo Shop { get; set; }

        [JsonProperty("attributes")]
        public ExTzItemAttributes Attributes { get; set; }

        [JsonProperty("properties")]
        public ExTzProperties Properties { get; set; }

        [JsonProperty("mods")]
        public Dictionary<string, ExTzItemMods> Mods { get; set; }
    }

    public class ExTzItemMods
    {
        [JsonProperty("implicit")]
        public Dictionary<string, object> Implicit { get; set; }

        [JsonProperty("explicit")]
        public Dictionary<string, object> Explicit { get; set; }
    }

    public class ExTzWeaponInfo
    {
        [JsonProperty("Physical DPS")]
        public double PhysicalDps { get; set; }

        [JsonProperty("Total DPS")]
        public double TotalDps { get; set; }

        [JsonProperty("Physical Damage")]
        public ExTzPropertyRange PhysicalDamage { get; set; }

        [JsonProperty("Total Damage")]
        public ExTzPropertyRange TotalDamage { get; set; }

        [JsonProperty("Critical Strike Chance")]
        public double CriticalStrikeChance { get; set; }

        [JsonProperty("Attacks per Second")]
        public double AttacksPerSecond { get; set; }
    }

    public class ExTzProperties
    {
        [JsonProperty("Weapon")]
        public ExTzWeaponInfo Weapon { get; set; }
    }

    public class ExTzPropertyRange
    {
        [JsonProperty("min")]
        public double Min { get; set; }

        [JsonProperty("avg")]
        public double Avg { get; set; }

        [JsonProperty("max")]
        public double Max { get; set; }
    }


    public class ExTzItemInfo
    {
        [JsonProperty("fullName")]
        public string FullName { get; set; }

        [JsonProperty("descrText")]
        public string Description { get; set; }

        [JsonProperty("typeLine")]
        public string TypeLine { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }
    }

    public class ExTzShopInfo
    {
        [JsonProperty("added")]
        public long AddedTimestampSerialized { get; set; }

        public DateTime AddedTimestamp
        {
            get { return DateTime.FromBinary(AddedTimestampSerialized); }
            set { AddedTimestampSerialized = value.ToBinary(); }
        }

        [JsonProperty("updated")]
        public long UpdatedTimestampSerialized { get; set; }

        public DateTime UpdatedTimestamp
        {
            get { return DateTime.FromBinary(UpdatedTimestampSerialized); }
            set { UpdatedTimestampSerialized = value.ToBinary(); }
        }

        [JsonProperty("modified")]
        public long ModifiedTimestampSerialized { get; set; }

        public DateTime ModifiedTimestamp
        {
            get { return DateTime.FromBinary(ModifiedTimestampSerialized); }
            set { ModifiedTimestampSerialized = value.ToBinary(); }
        }

        [JsonProperty("chaosEquiv")]
        public double ChaosEquivalent { get; set; }

        [JsonProperty("hasPrice")]
        public bool HasPrice { get; set; }

        [JsonProperty("verified")]
        public string Verified { get; set; }

        [JsonProperty("price")]
        public ExTzItemPrice Price { get; set; }
    }

    public class ExTzItemPrice
    {
        [JsonProperty("mods")]
        public Dictionary<string, ExTzItemMods> Mods { get; set; }
    }

    public class ExTzItemSockets
    {
        [JsonProperty("allSockets")]
        public string AllSockets { get; set; }
    }

    public class ExTzItemAttributes
    {
        [JsonProperty("league")]
        public string League { get; set; }

        [JsonProperty("itemType")]
        public string ItemType { get; set; }

        [JsonProperty("weaponType")]
        public string WeaponType { get; set; }

        [JsonProperty("equipType")]
        public string EquipType { get; set; }

        [JsonProperty("rarity")]
        public string Rarity { get; set; }

        [JsonProperty("baseItemType")]
        public string BaseItemType { get; set; }

        [JsonProperty("ilvl")]
        public int ItemLevel { get; set; }

        [JsonProperty("corrupted")]
        public bool IsCorrupted { get; set; }

        [JsonProperty("identified")]
        public bool IsIdentified { get; set; }
    }
}