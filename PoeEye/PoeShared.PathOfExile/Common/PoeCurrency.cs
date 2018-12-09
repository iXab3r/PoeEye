namespace PoeShared.Common
{
    public sealed class PoeCurrency : IPoeCurrency
    {
        public string CodeName { get; set; }

        public string Name { get; set; }

        public string IconUri { get; set; }
    }
}