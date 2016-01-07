namespace PoeShared.Common
{
    using PoeTrade.Query;

    public sealed class PoeQueryModsGroup : IPoeQueryModsGroup
    {
        public float? Min { get; set; }

        public float? Max { get; set; }

        public IPoeQueryRangeModArgument[] Mods { get; set; }

        public PoeQueryModsGroupType GroupType { get; set; }
    }
}