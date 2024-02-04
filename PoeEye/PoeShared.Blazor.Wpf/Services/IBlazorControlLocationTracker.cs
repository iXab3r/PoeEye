using System.ComponentModel;
using System.Drawing;

namespace PoeShared.Blazor.Wpf.Services;

public interface IBlazorControlLocationTracker : INotifyPropertyChanged
{
    public Rectangle BoundsOnScreen { get; } 
}