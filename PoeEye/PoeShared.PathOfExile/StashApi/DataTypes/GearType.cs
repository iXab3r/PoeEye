using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PoeShared.StashApi.DataTypes
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum GearType
    {
        Unknown,
        Ring,
        Amulet,
        Helmet,
        Chest,
        Belt,
        Gloves,
        Boots,
        Axe,
        Claw,
        Bow,
        Dagger,
        Mace,
        Quiver,
        Sceptre,
        Staff,
        Sword,
        Shield,
        Wand,
        Flask,
        Map,
        QuestItem,
        DivinationCard,
        Jewel
    }
}