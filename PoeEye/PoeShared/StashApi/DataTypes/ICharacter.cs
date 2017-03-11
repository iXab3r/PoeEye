namespace PoeShared.StashApi.DataTypes
{
    public interface ICharacter
    {
        string Class { get; }

        int ClassId { get; }

        string League { get; }

        int Level { get; }

        string Name { get; }
    }
}