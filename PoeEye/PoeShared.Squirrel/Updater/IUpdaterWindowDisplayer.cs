namespace PoeShared.Squirrel.Updater
{
    public interface IUpdaterWindowDisplayer
    {
        bool? ShowDialog(UpdaterWindowArgs args);
    }
}