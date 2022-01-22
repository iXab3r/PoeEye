using PoeShared.Scaffolding;
using Squirrel;

namespace PoeShared.Squirrel.Updater;

public interface IUpdaterWindowViewModel : IDisposableReactiveObject
{
    IUpdateSourceProvider UpdateSourceProvider { get; }
    string Title { get; }
    string Message { get; }
    IApplicationUpdaterViewModel ApplicationUpdater { get; }
    IReleaseEntry SelectedReleaseEntry { get; set; }
}