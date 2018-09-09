using RestSharp.Deserializers;

namespace PoeShared.StashApi.DataTypes
{
    public sealed class StashItemSocket
    {
        [DeserializeAs(Name = "attr")]
        public string Attribute { get; set; }

        [DeserializeAs(Name = "group")]
        public int Group { get; set; }
    }
}