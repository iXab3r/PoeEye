using System.Collections.Generic;

namespace PoeShared.StashApi.DataTypes
{
    public interface IInventory
    {
        IEnumerable<IStashItem> Items { get; }
    }
}