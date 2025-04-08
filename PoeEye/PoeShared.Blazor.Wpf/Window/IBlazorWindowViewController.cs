using System.Windows;
using PoeShared.Native;
using PoeShared.UI;

namespace PoeShared.Blazor.Wpf;

public interface IBlazorWindowViewController : IWindowViewController
{
    Window Window { get; }
    
    IBlazorWindow BlazorWindow { get; }
    
    void EnsureCreated();
}