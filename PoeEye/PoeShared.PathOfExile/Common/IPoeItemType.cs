namespace PoeShared.Common
{
    public interface IPoeItemType
    {
        string CodeName { get; }

        string Name { get; }

        string ItemType { get; set; }

        string EquipType { get; set; }
    }
}