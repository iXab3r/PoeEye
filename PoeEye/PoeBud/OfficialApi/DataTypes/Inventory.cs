using System.Collections.Generic;
using RestSharp.Deserializers;

namespace PoeBud.OfficialApi.DataTypes
{
    internal class Inventory : IInventory
    {
        [DeserializeAs(Name = "items")]
        public List<Item> Items { get; set; }

        IEnumerable<IItem> IInventory.Items => Items;
    }
}
