using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PoeEye.ExileToolsApi.Extensions;
using PoeShared.Common;

namespace PoeEye.ExileToolsApi.Entities
{
    [ElasticsearchType(Name = "item")]
    internal class ExTzItem
    {
        [JsonProperty("info")]
        public ExTzItemInfo Info { get; set; }

        [JsonProperty("shop")]
        public ExTzShopInfo Shop { get; set; }

        [JsonProperty("uuid")]
        public string ItemId { get; set; }

        [JsonProperty("attributes")]
        public ExTzItemAttributes Attributes { get; set; }

        [JsonProperty("properties")]
        public ExTzProperties Properties { get; set; }

        [JsonProperty("propertiesPseudo")]
        public ExTzPropertiesPseudo PropertiesPseudo { get; set; }

        [JsonProperty("mods")]
        public Dictionary<string, ExTzItemMods> Mods { get; set; }

        [JsonProperty("modsPseudo")]
        public Dictionary<string, object> ModsPseudo { get; set; }

        [JsonProperty("sockets")]
        public ExTzItemSockets Sockets { get; set; }

        [JsonProperty("requirements")]
        public ExTzItemRequirements Requirements { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    internal enum VerificationStatus
    {
        [EnumMember(Value = "YES")]
        Yes,
        [EnumMember(Value = "GONE")]
        Gone,
        [EnumMember(Value = "OLD")]
        Old
    }

    internal class ExTzItemMods
    {
        [JsonProperty("implicit")]
        public Dictionary<string, object> Implicit { get; set; }

        [JsonProperty("explicit")]
        public Dictionary<string, object> Explicit { get; set; }
    }

    internal class ExTzItemRequirements
    {
        [JsonProperty("Str")]
        public double Strength { get; set; }

        [JsonProperty("Dex")]
        public double Dexterity { get; set; }

        [JsonProperty("Int")]
        public double Intelligence { get; set; }

        [JsonProperty("Level")]
        public double Level { get; set; }
    }

    internal class ExTzArmourInfo
    {
        [JsonProperty("Armour")]
        public double Armour { get; set; }

        [JsonProperty("Energy Shield")]
        public double EnergyShield { get; set; }

        [JsonProperty("Evasion")]
        public double Evasion { get; set; }

        [JsonProperty("Chance to Block")]
        public double BlockChance { get; set; }
    }

    internal class ExTzGemInfo
    {
        [JsonProperty("Level")]
        public double Level { get; set; }
    }

    internal class ExTzWeaponInfo
    {
        [JsonProperty("Physical DPS")]
        public double PhysicalDps { get; set; }

        [JsonProperty("Elemental DPS")]
        public double ElementalDps { get; set; }

        [JsonProperty("Total DPS")]
        public double TotalDps { get; set; }

        [JsonProperty("Elemental Damage")]
        public ExTzPropertyRange ElementalDamage { get; set; }

        [JsonProperty("Physical Damage")]
        public ExTzPropertyRange PhysicalDamage { get; set; }

        [JsonProperty("Total Damage")]
        public ExTzPropertyRange TotalDamage { get; set; }

        [JsonProperty("Critical Strike Chance")]
        public double CriticalStrikeChance { get; set; }

        [JsonProperty("Attacks per Second")]
        public double AttacksPerSecond { get; set; }
    }

    internal class ExTzProperties
    {
        [JsonProperty("Weapon")]
        public ExTzWeaponInfo Weapon { get; set; }

        [JsonProperty("Armour")]
        public ExTzArmourInfo Armour { get; set; }

        [JsonProperty("Gem")]
        public ExTzGemInfo Gem { get; set; }

        [JsonProperty("Quality")]
        public double Quality { get; set; }
    }

    internal class ExTzPropertiesPseudo
    {
        [JsonProperty("Weapon")]
        public ExTzPropertiesPseudoWeapon Weapon { get; set; }

        [JsonProperty("Armour")]
        public ExTzPropertiesPseudoArmour Armour { get; set; }
    }

    internal class ExTzPropertiesPseudoWeapon
    {
        [JsonProperty("estimatedQ20")]
        public ExTzPropertiesPseudoWeaponQ20 Q20 { get; set; }
    }

    internal class ExTzPropertiesPseudoWeaponQ20
    {
        [JsonProperty("Physical DPS")]
        public double PhysicalDps { get; set; }

        [JsonProperty("Total DPS")]
        public double TotalDps { get; set; }
    }

    internal class ExTzPropertiesPseudoArmour
    {
        [JsonProperty("estimatedQ20")]
        public ExTzPropertiesPseudoArmourQ20 Q20 { get; set; }
    }

    internal class ExTzPropertiesPseudoArmourQ20
    {
        [JsonProperty("Armour")]
        public double Armour { get; set; }

        [JsonProperty("Energy Shield")]
        public double EnergyShield { get; set; }

        [JsonProperty("Evasion")]
        public double Evasion { get; set; }
    }

    internal struct ExTzPropertyRange
    {
        [JsonProperty("min")]
        public double Min { get; set; }

        [JsonProperty("avg")]
        public double Avg { get; set; }

        [JsonProperty("max")]
        public double Max { get; set; }

        public override string ToString()
        {
            return $"{Min} - {Max}";
        }
    }

    internal class ExTzItemInfo
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

    internal class ExTzShopInfo
    {
        [JsonProperty("added")]
        public long AddedTimestampSerialized { get; set; }

        [JsonProperty("verified")]
        public VerificationStatus Status { get; set; }

        [JsonIgnore]
        public DateTime AddedTimestamp
        {
            get { return AddedTimestampSerialized.ToUnixTimeStamp(); }
            set { AddedTimestampSerialized = value.ToUnixTimeStampInMilliseconds(); }
        }

        [JsonProperty("updated")]
        public long UpdatedTimestampSerialized { get; set; }

        [JsonIgnore]
        public DateTime UpdatedTimestamp
        {
            get { return UpdatedTimestampSerialized.ToUnixTimeStamp(); }
            set { UpdatedTimestampSerialized = value.ToUnixTimeStampInMilliseconds(); }
        }

        [JsonProperty("modified")]
        public long ModifiedTimestampSerialized { get; set; }

        [JsonIgnore]
        public DateTime ModifiedTimestamp
        {
            get { return ModifiedTimestampSerialized.ToUnixTimeStamp(); }
            set { ModifiedTimestampSerialized = value.ToUnixTimeStampInMilliseconds(); }
        }

        [JsonProperty("chaosEquiv")]
        public double ChaosEquivalent { get; set; }

        [JsonProperty("hasPrice")]
        public bool HasPrice { get; set; }

        [JsonProperty("sellerAccount")]
        public string SellerAccount { get; set; }

        [JsonProperty("note")]
        public string Note { get; set; }

        [JsonProperty("currency")]
        public string CurrencyRequested { get; set; }

        [JsonProperty("amount")]
        public double AmountRequested { get; set; }

        [JsonProperty("lastCharacterName")]
        public string LastCharacterName { get; set; }

        [JsonProperty("defaultMessage")]
        public string DefaultMessage { get; set; }

        [JsonProperty("price")]
        public ExTzItemPrice Price { get; set; }
    }

    internal class ExTzItemPrice
    {
        [JsonProperty("mods")]
        public Dictionary<string, ExTzItemMods> Mods { get; set; }
    }

    internal class ExTzItemSockets
    {
        [JsonProperty("allSocketsGGG")]
        public string Raw { get; set; }
    }

    internal class ExTzItemAttributes
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

        [JsonProperty("mirrored")]
        public bool IsMirrored { get; set; }
    }

    internal enum KnownItemType
    {
        Unknown,
        Gem
    }
}