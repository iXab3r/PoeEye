using System.Collections.Generic;

namespace PoeBud.OfficialApi.DataTypes
{
    public interface IItem
    {
        List<AdditionalProperty> additionalProperties { get; }

        string Color { get; }

        bool Corrupted { get; }

        List<string> CosmeticMods { get; }

        List<string> CraftedMods { get; }

        string DescrText { get; }

        List<string> ExplicitMods { get; }

        List<string> FlavourText { get; }

        PoeItemRarity Rarity { get; }

        int H { get; }

        string Icon { get; }

        bool Identified { get; }

        List<string> ImplicitMods { get; }

        string InventoryId { get; }

        string League { get; }

        string Name { get; }

        List<Requirement> nextLevelRequirements { get; }

        List<Property> Properties { get; }

        List<Requirement> Requirements { get; }

        string SecDescrText { get; }

        List<Item> SocketedItems { get; }

        List<Socket> Sockets { get; }

        bool Support { get; }

        string TypeLine { get; }

        bool Verified { get; }

        GearType ItemType { get; }

        int W { get; }

        int X { get; }

        int Y { get; }
    }
}