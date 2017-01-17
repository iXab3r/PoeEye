using RestSharp.Deserializers;

namespace PoeBud.OfficialApi.DataTypes
{
    internal class Character : ICharacter
    {
        [DeserializeAs(Name = "name")]
        public string Name { get; set; }

        [DeserializeAs(Name = "league")]
        public string League { get; set; }

        [DeserializeAs(Name = "class")]
        public string Class { get; set; }

        [DeserializeAs(Name = "classId")]
        public int ClassId { get; set; }

        [DeserializeAs(Name = "level")]
        public int Level { get; set; }

        public override string ToString()
        {
            return $"[Character] Name: {Name}, League: {League}, Class: {Class}, ClassId: {ClassId}, Level: {Level}";
        }
    }
}
