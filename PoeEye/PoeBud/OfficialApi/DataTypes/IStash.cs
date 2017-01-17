using System.Collections.Generic;

namespace PoeBud.OfficialApi.DataTypes
{
    internal interface IStash
    {
        int NumTabs { get; }

        IEnumerable<IItem> Items { get; }

        IEnumerable<ITab> Tabs { get; }
    }
}