namespace PoeShared.Common
{
    public sealed class PoeItemMod : IPoeItemMod
    {
        public PoeModType ModType { get; set; }

        public string Name { get; set; }

        public string CodeName { get; set; }

        public bool IsCrafted { get; set; }

        public override string ToString()
        {
            return $"ModType: {ModType}, CodeName: {CodeName}, IsCrafted: {IsCrafted}";
        }
    }
}