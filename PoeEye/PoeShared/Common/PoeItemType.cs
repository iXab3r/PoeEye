using Guards;

namespace PoeShared.Common
{
    public sealed class PoeItemType : IPoeItemType
    {
        public string CodeName { get; set; }

        public string ItemType { get; set; }

        public string EquipType { get; set; }

        public string Name { get; set; }

        public PoeItemType() : this(string.Empty, string.Empty) {}

        public PoeItemType(string name) : this(name, name) {}

        public PoeItemType(string name, string codeName)
        {
            Guard.ArgumentNotNull(name, nameof(name));
            Guard.ArgumentNotNull(codeName, nameof(codeName));

            Name = name;
            CodeName = codeName;
        }

        public override string ToString()
        {
            return Name ?? base.ToString();
        }
    }
}
