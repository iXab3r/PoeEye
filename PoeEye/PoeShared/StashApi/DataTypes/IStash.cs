using System.Collections.Generic;

namespace PoeShared.StashApi.DataTypes
{
    public interface IStash
    {
        int NumTabs { get; }

        IEnumerable<IStashItem> Items { get; }

        IEnumerable<IStashTab> Tabs { get; }
    }
}