namespace PoeShared.Common
{
    public interface IPoeItemMod
    {
        PoeModType ModType { get; }

        string Name { get; }

        string CodeName { get; }

        bool IsCrafted { get; }
    }
}