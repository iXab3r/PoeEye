using System.ComponentModel;

namespace PoeShared.Native;

public interface IHasVisible : INotifyPropertyChanged
{
    bool IsVisible { get; }
}

public interface ICanBeVisible : IHasVisible
{
    new bool IsVisible { get; set; }
}