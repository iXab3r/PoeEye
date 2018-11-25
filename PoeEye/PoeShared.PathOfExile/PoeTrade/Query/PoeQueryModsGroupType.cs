using System.ComponentModel;

namespace PoeShared.PoeTrade.Query
{
    public enum PoeQueryModsGroupType
    {
        [Browsable(false)]
        Unknown,
        And,
        Not,
        Count,
        Sum,
        If
    }
}