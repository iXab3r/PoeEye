using System.ComponentModel;

namespace PoeShared.Scaffolding;

public abstract record ReactiveRecord : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
}