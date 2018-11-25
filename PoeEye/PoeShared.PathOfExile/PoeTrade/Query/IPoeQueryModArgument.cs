using PoeShared.Common;

namespace PoeShared.PoeTrade.Query
{
    public interface IPoeQueryModArgument : IPoeQueryArgument
    {
        IPoeItemMod Mod { get; }
    }
}