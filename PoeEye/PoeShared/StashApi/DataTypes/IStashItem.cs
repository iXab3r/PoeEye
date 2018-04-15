using System.Collections.Generic;
using PoeShared.Common;

namespace PoeShared.StashApi.DataTypes
{
    public interface IStashItem
    {
        List<StashItemAdditionalProperty> additionalProperties { get; }

        string Id { get; set; }

        string Note { get; set; }

        string Color { get; }

        int ItemLevel { get; }

        bool Corrupted { get; }

        List<string> CosmeticMods { get; }

        List<string> CraftedMods { get; }

        string DescrText { get; }

        List<string> ExplicitMods { get; }

        List<string> FlavourText { get; }

        PoeItemRarity Rarity { get; }

        int Height { get; }

        string Icon { get; }

        bool Identified { get; }

        List<string> ImplicitMods { get; }

        string InventoryId { get; }

        string League { get; }

        string Name { get; }

        List<StashItemRequirement> nextLevelRequirements { get; }

        List<StashItemProperty> Properties { get; }

        List<StashItemRequirement> Requirements { get; }

        string SecDescrText { get; }

        List<StashItem> SocketedItems { get; }

        List<StashItemSocket> Sockets { get; }

        bool Support { get; }

        string TypeLine { get; }
        
        IEnumerable<string> Categories { get; }

        bool Verified { get; }

        GearType ItemType { get; }

        int Width { get; }

        int X { get; }

        int Y { get; }
        
        int StackSize { get; }
        
        int MaxStackSize { get; }
        
        ItemPosition Position { get; }
    }
}