using System.Collections.Generic;

namespace PoeBud.OfficialApi.DataTypes
{
    public interface IInventory
    {
        IEnumerable<IItem> Items { get; }
    }
}