namespace PoeShared.PoeTrade.Query
{
    public interface IPoeQueryModsGroup
    {
        float? Min { get; }

        float? Max { get; }

        IPoeQueryRangeModArgument[] Mods { get; }

        PoeQueryModsGroupType GroupType { get; }
    }
}