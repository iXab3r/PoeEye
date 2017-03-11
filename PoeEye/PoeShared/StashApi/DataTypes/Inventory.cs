using System.Collections.Generic;
using RestSharp.Deserializers;

namespace PoeShared.StashApi.DataTypes
{
    internal class Inventory : IInventory
    {
        [DeserializeAs(Name = "items")]
        public List<StashItem> Items { get; set; }

        IEnumerable<IStashItem> IInventory.Items => Items;
    }
}
