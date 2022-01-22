using System.ComponentModel;

namespace PoeShared.Scaffolding;

public interface ICloseable : INotifyPropertyChanged
{
    public ICloseController CloseController { get; set; }
}
    
public interface ICloseable<T> : INotifyPropertyChanged
{
    public ICloseController<T> CloseController { get; set; }
}