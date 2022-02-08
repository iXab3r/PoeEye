namespace PoeShared.Scaffolding;

public interface ISuspendable
{
    IDisposable Suspend();
}