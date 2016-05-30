using PoeShared.Common;

namespace PoeShared.PoeTrade.Query
{
    using JetBrains.Annotations;

    public class PoeQueryRangeModArgument : PoeQueryModArgument, IPoeQueryRangeModArgument
    {
        public PoeQueryRangeModArgument(IPoeItemMod mod) : base(mod)
        {
        }

        public float? Min { get; set; }

        public float? Max { get; set; }
    }
}