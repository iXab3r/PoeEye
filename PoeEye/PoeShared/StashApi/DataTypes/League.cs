using System;
using RestSharp.Deserializers;

namespace PoeShared.StashApi.DataTypes
{
    public sealed class League : ILeague
    {
        [DeserializeAs(Name = "id")]
        public string Id { get; set; }

        [DeserializeAs(Name = "description")]
        public string Description { get; set; }

        [DeserializeAs(Name = "startAt")]
        public DateTime StartAt { get; set; }

        [DeserializeAs(Name = "endAt")]
        public DateTime EndAt { get; set; }

        public override string ToString()
        {
            return $"[League] Name: {Id}, StartAt: {StartAt}, EndAt: {EndAt}";
        }
    }
}