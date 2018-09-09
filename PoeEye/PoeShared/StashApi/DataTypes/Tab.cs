using System;
using RestSharp.Deserializers;

namespace PoeShared.StashApi.DataTypes
{
    internal class Tab : IStashTab
    {
        [DeserializeAs(Name = "type")]
        public string StashTypeName { get; set; }

        [DeserializeAs(Name = "n")]
        public string Name { get; set; }

        [DeserializeAs(Name = "i")]
        public int Idx { get; set; }

        [DeserializeAs(Name = "colour")]
        public Colour Colour { get; set; }

        public StashTabType StashType
        {
            get => Parse(StashTypeName);
            set => StashTypeName = StashType.ToString();
        }

        public string srcL { get; set; }

        public string srcC { get; set; }

        public string srcR { get; set; }

        [DeserializeAs(Name = "hidden")]
        public bool Hidden { get; set; }

        [DeserializeAs(Name = "id")]
        public string Id { get; set; }

        internal StashTabType Parse(string stashTypeName)
        {
            StashTabType result;
            var parsed = Enum.TryParse(stashTypeName, out result);
            return parsed ? result : StashTabType.Unknown;
        }
    }
}