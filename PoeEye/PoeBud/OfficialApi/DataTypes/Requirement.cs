using System.Collections.Generic;
using RestSharp.Deserializers;

namespace PoeBud.OfficialApi.DataTypes
{
    public class Requirement
    {
        [DeserializeAs(Name = "name")]
        public string Name { get; set; }

        [DeserializeAs(Name = "values")]
        public List<object> Value { get; set; }

        [DeserializeAs(Name = "displayMode")]
        public int DisplayMode { get; set; }
    }
}