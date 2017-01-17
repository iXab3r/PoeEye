using RestSharp.Deserializers;

namespace PoeBud.OfficialApi.DataTypes
{
    internal class Tab : ITab
    {
        [DeserializeAs(Name = "n")]
        public string Name { get; set; }

        [DeserializeAs(Name = "i")]
        public int Idx { get; set; }

        public Colour colour { get; set; }

        [DeserializeAs(Name = "type")]
        public string StashTypeName { get; set; }

        public string srcL { get; set; }

        public string srcC { get; set; }

        public string srcR { get; set; }

        public bool hidden { get; set; }
    }
}
