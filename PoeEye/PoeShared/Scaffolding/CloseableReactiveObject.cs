namespace PoeShared.Scaffolding;

public abstract class CloseableReactiveObject : DisposableReactiveObject, ICloseable
{

    public ICloseController CloseController { get; set; }
}