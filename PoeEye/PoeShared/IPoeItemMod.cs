namespace PoeShared
{
    public interface IPoeItemMod
    {
        PoeModType ModType { get; }

        string Name { get; }

        string CodeName { get; }
    }
}