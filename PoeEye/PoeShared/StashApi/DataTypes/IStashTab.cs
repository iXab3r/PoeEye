namespace PoeShared.StashApi.DataTypes
{
    public interface IStashTab
    {
        Colour Colour { get; set; }

        bool Hidden { get; set; }

        int Idx { get; set; }

        StashTabType StashType { get; set; }

        string Name { get; set; }

        string srcC { get; set; }

        string srcL { get; set; }

        string srcR { get; set; }

        string Id { get; set; }
    }
}