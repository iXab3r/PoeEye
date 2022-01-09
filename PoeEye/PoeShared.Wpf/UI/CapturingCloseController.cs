using PoeShared.Scaffolding;

namespace PoeShared.UI;

public class CapturingCloseController<T> : ICloseController<T>
{
    public T Result { get; private set; }
        
    public void Close(T value)
    {
        Result = value;
    }
}