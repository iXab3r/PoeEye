using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using PoeShared.Common;
using PoeShared.Scaffolding;
using RestSharp.Deserializers;

namespace PoeShared.StashApi.DataTypes
{
    public sealed class StashItem : IStashItem
    {
        [DeserializeAs(Name = "id")]
        [JsonProperty("id")]
        public string Id { get; set; }

        public IEnumerable<string> Categories => CategoriesRaw == null ? Enumerable.Empty<string>() : CategoriesRaw.Keys.EmptyIfNull();
        
        [DeserializeAs(Name = "category")]
        [JsonProperty("category")]
        public Dictionary<string, string> CategoriesRaw { get; set; }

        [DeserializeAs(Name = "verified")]
        [JsonProperty("verified")]
        public bool Verified { get; set; }

        [DeserializeAs(Name = "w")]
        [JsonProperty("w")]
        public int Width { get; set; }

        [DeserializeAs(Name = "h")]
        [JsonProperty("h")]
        public int Height { get; set; }

        [DeserializeAs(Name = "icon")]
        [JsonProperty("icon")]
        public string Icon { get; set; }

        [DeserializeAs(Name = "note")]
        [JsonProperty("note")]
        public string Note { get; set; }

        [DeserializeAs(Name = "support")]
        [JsonProperty("support")]
        public bool Support { get; set; }

        [DeserializeAs(Name = "league")]
        [JsonProperty("league")]
        public string League { get; set; }

        [DeserializeAs(Name = "ilvl")]
        [JsonProperty("ilvl")]
        public int ItemLevel { get; set; }

        [DeserializeAs(Name = "name")]
        [JsonProperty("name")]
        public string Name { get; set; }

        [DeserializeAs(Name = "typeLine")]
        [JsonProperty("typeLine")]
        public string TypeLine { get; set; }

        [DeserializeAs(Name = "identified")]
        [JsonProperty("identified")]
        public bool Identified { get; set; }

        [DeserializeAs(Name = "properties")]
        [JsonProperty("properties")]
        public List<StashItemProperty> Properties { get; set; }

        [DeserializeAs(Name = "explicitMods")]
        [JsonProperty("explicitMods")]
        public List<string> ExplicitMods { get; set; }

        [DeserializeAs(Name = "descrText")]
        [JsonProperty("descrText")]
        public string DescrText { get; set; }

        [DeserializeAs(Name = "frameType")]
        [JsonProperty("frameType")]
        public PoeItemRarityWrapper RarityWrapper { get; set; }

        [DeserializeAs(Name = "x")]
        [JsonProperty("x")]
        public int X { get; set; }

        [DeserializeAs(Name = "y")]
        [JsonProperty("y")]
        public int Y { get; set; }

        [DeserializeAs(Name = "inventoryId")]
        [JsonProperty("inventoryId")]
        public string InventoryId { get; set; }

        [DeserializeAs(Name = "socketedItems")]
        [JsonProperty("socketedItems")]
        public List<StashItem> SocketedItems { get; set; }

        [DeserializeAs(Name = "sockets")]
        [JsonProperty("sockets")]
        public List<StashItemSocket> Sockets { get; set; }

        [DeserializeAs(Name = "additionalProperties")]
        [JsonProperty("additionalProperties")]
        public List<StashItemAdditionalProperty> additionalProperties { get; set; }

        [DeserializeAs(Name = "secDescrText")]
        [JsonProperty("secDescrText")]
        public string SecDescrText { get; set; }

        [DeserializeAs(Name = "implicitMods")]
        [JsonProperty("implicitMods")]
        public List<string> ImplicitMods { get; set; }

        [DeserializeAs(Name = "flavourText")]
        [JsonProperty("flavourText")]
        public List<string> FlavourText { get; set; }

        [DeserializeAs(Name = "requirements")]
        [JsonProperty("requirements")]
        public List<StashItemRequirement> Requirements { get; set; }

        [DeserializeAs(Name = "nextLevelRequirements")]
        [JsonProperty("nextLevelRequirements")]
        public List<StashItemRequirement> nextLevelRequirements { get; set; }

        [DeserializeAs(Name = "socket")]
        [JsonProperty("socket")]
        public int Socket { get; set; }

        [DeserializeAs(Name = "colour")]
        [JsonProperty("colour")]
        public string Color { get; set; }

        [DeserializeAs(Name = "corrupted")]
        [JsonProperty("corrupted")]
        public bool Corrupted { get; set; }

        [DeserializeAs(Name = "cosmeticMods")]
        [JsonProperty("cosmeticMods")]
        public List<string> CosmeticMods { get; set; }

        [DeserializeAs(Name = "craftedMods")]
        [JsonProperty("craftedMods")]
        public List<string> CraftedMods { get; set; }
        
        [DeserializeAs(Name = "stackSize")]
        [JsonProperty("stackSize")]
        public int StackSize { get; set; }
        
        [DeserializeAs(Name = "maxStackSize")]
        [JsonProperty("maxStackSize")]
        public int MaxStackSize { get; set; }
        
        public GearType ItemType { get; set; }

        public PoeItemRarity Rarity => (PoeItemRarity)((int)RarityWrapper + 1);
        
        public ItemPosition Position => new ItemPosition(x: X, y: Y, width: Width, height: Height);

        public StashItem CleanupItemName()
        {
            if (!string.IsNullOrWhiteSpace(Name))
            {
                Name = Regex.Replace(Name, @"\<.*\>", string.Empty);
            }
            return this;
        }

        public override string ToString()
        {
            var name = !string.IsNullOrEmpty(Name)
                ? Name
                : TypeLine;

            return $"{name}@{InventoryId}-X{X}Y{Y}";
        }
    }
}
