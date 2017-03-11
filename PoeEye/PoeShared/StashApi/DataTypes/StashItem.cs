using System.Collections.Generic;
using PoeShared.Common;
using RestSharp.Deserializers;

namespace PoeShared.StashApi.DataTypes
{
    public sealed class StashItem : IStashItem
    {
        [DeserializeAs(Name = "verified")]
        public bool Verified { get; set; }

        [DeserializeAs(Name = "w")]
        public int W { get; set; }

        [DeserializeAs(Name = "h")]
        public int H { get; set; }

        [DeserializeAs(Name = "icon")]
        public string Icon { get; set; }

        [DeserializeAs(Name = "support")]
        public bool Support { get; set; }

        [DeserializeAs(Name = "league")]
        public string League { get; set; }

        [DeserializeAs(Name = "name")]
        public string Name { get; set; }

        [DeserializeAs(Name = "typeLine")]
        public string TypeLine { get; set; }

        [DeserializeAs(Name = "identified")]
        public bool Identified { get; set; }

        [DeserializeAs(Name = "properties")]
        public List<StashItemProperty> Properties { get; set; }

        [DeserializeAs(Name = "explicitMods")]
        public List<string> ExplicitMods { get; set; }

        [DeserializeAs(Name = "descrText")]
        public string DescrText { get; set; }

        [DeserializeAs(Name = "frameType")]
        public PoeItemRarityWrapper RarityWrapper { get; set; }

        public PoeItemRarity Rarity => (PoeItemRarity) ((int) RarityWrapper + 1);

        [DeserializeAs(Name = "x")]
        public int X { get; set; }

        [DeserializeAs(Name = "y")]
        public int Y { get; set; }

        [DeserializeAs(Name = "inventoryId")]
        public string InventoryId { get; set; }

        [DeserializeAs(Name = "socketedItems")]
        public List<StashItem> SocketedItems { get; set; }

        [DeserializeAs(Name = "sockets")]
        public List<StashItemSocket> Sockets { get; set; }

        [DeserializeAs(Name = "additionalProperties")]
        public List<StashItemAdditionalProperty> additionalProperties { get; set; }

        [DeserializeAs(Name = "secDescrText")]
        public string SecDescrText { get; set; }

        [DeserializeAs(Name = "implicitMods")]
        public List<string> ImplicitMods { get; set; }

        [DeserializeAs(Name = "flavourText")]
        public List<string> FlavourText { get; set; }

        [DeserializeAs(Name = "requirements")]
        public List<StashItemRequirement> Requirements { get; set; }

        [DeserializeAs(Name = "nextLevelRequirements")]
        public List<StashItemRequirement> nextLevelRequirements { get; set; }

        [DeserializeAs(Name = "socket")]
        public int Socket { get; set; }

        [DeserializeAs(Name = "colour")]
        public string Color { get; set; }

        [DeserializeAs(Name = "corrupted")]
        public bool Corrupted { get; set; }

        [DeserializeAs(Name = "cosmeticMods")]
        public List<string> CosmeticMods { get; set; }

        [DeserializeAs(Name = "craftedMods")]
        public List<string> CraftedMods { get; set; }

        public GearType ItemType { get; set; }

        public override string ToString()
        {
            var name = !string.IsNullOrEmpty(Name)
                ? Name
                : TypeLine;

            return $"{name}@{InventoryId}-X{X}Y{Y}";
        }
    }
}
