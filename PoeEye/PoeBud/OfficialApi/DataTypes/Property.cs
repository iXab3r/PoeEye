using System.Collections.Generic;
using RestSharp.Deserializers;

namespace PoeBud.OfficialApi.DataTypes
{
    internal class Property
    {
        [DeserializeAs(Name = "name")]
        public string Name { get; set; }

        [DeserializeAs(Name = "values")]
        public List<object> Values { get; set; }

        [DeserializeAs(Name = "displayMode")]
        public int DisplayMode { get; set; }
    }
}
