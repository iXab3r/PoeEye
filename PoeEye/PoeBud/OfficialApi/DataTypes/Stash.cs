using System.Collections.Generic;
using RestSharp.Deserializers;

namespace PoeBud.OfficialApi.DataTypes
{
    public class Stash : IStash
    {
        [DeserializeAs(Name = "numTabs")]
        public int NumTabs { get; set; }

        [DeserializeAs(Name = "items")]
        public List<Item> Items { get; set; }

        [DeserializeAs(Name = "tabs")]
        public List<Tab> Tabs { get; set; }

        IEnumerable<IItem> IStash.Items => Items;

        IEnumerable<ITab> IStash.Tabs => Tabs;
    }
}