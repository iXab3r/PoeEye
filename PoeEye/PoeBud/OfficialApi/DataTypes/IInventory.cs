using System.Collections.Generic;

namespace PoeBud.OfficialApi.DataTypes
{
    internal interface IInventory
    {
        IEnumerable<IItem> Items { get; }
    }
}