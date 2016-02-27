namespace PoeShared.Common
{
    public interface IPoeCurrency
    {
        string CodeName { get; }

        string Name { get; }

        string IconUri { get; }
    }
}