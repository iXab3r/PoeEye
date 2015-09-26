namespace PoeEye.Common
{
    using PoeShared.Common;

    [ToString]
    internal sealed class PoeCurrency : IPoeCurrency
    {
        public string CodeName { get; set; }

        public string Name { get; set; }

        public string IconUri { get; set; }
    }
}