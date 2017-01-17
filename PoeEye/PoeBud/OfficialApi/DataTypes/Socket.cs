using RestSharp.Deserializers;

namespace PoeBud.OfficialApi.DataTypes
{
    internal class Socket
    {
        [DeserializeAs(Name = "attr")]
        public string Attribute { get; set; }

        [DeserializeAs(Name = "group")]
        public int Group { get; set; }
    }
}
