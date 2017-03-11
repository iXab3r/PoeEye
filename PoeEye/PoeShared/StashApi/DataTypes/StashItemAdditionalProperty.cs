using System.Collections.Generic;
using RestSharp.Deserializers;

namespace PoeShared.StashApi.DataTypes
{
    public sealed class StashItemAdditionalProperty
    {
        [DeserializeAs(Name = "name")]
        public string name { get; set; }

        [DeserializeAs(Name = "values")]
        public List<List<object>> values { get; set; }

        [DeserializeAs(Name = "displayMode")]
        public int displayMode { get; set; }

        [DeserializeAs(Name = "progress")]
        public double progress { get; set; }
    }
}
