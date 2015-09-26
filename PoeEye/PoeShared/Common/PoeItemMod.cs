namespace PoeShared.Common
{
    public sealed class PoeItemMod : IPoeItemMod
    {
        public PoeModType ModType { get; set; }

        public string Name { get; set; }

        public string CodeName { get; set; }
    }
}