using Guards;
using PoeShared.Common;

namespace PoeShared.PoeTrade.Query
{
    public class PoeQueryModArgument : PoeQueryArgumentBase, IPoeQueryModArgument
    {
        public PoeQueryModArgument(IPoeItemMod mod) : base(mod.Name)
        {
            Guard.ArgumentNotNull(() => mod);
            Mod = mod;
        }

        public bool Excluded { get; set; }

        public IPoeItemMod Mod { get; set; }
    }
}