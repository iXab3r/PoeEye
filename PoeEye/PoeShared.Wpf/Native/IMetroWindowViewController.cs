namespace PoeShared.Native;

public interface IMetroWindowViewController : IWindowViewController
{
    ReactiveMetroWindow Window { get; }
}