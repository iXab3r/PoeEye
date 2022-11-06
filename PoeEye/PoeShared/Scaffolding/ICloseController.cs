namespace PoeShared.Scaffolding;

public interface ICloseController
{
    void Close();
}
    
public interface ICloseController<in TValue> : ICloseController
{
    void Close(TValue value);
}