namespace PoeShared.Common
{
    public sealed class PoeItemType : IPoeItemType
    {
        public string CodeName { get; set; }

        public string ItemType { get; set; }

        public string EquipType { get; set; }

        public string Name { get; set; }

        public override string ToString()
        {
            return Name ?? base.ToString();
        }
    }
}