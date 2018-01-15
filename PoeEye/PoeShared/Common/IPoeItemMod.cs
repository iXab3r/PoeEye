namespace PoeShared.Common
{
    public interface IPoeItemMod
    {
        PoeModType ModType { get; }
        
        PoeModOrigin Origin { get; }

        string Name { get; }

        string CodeName { get; }

        string TierInfo { get; }
    }
}