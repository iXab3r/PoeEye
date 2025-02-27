using PoeShared.Native;
using PoeShared.UI;

namespace PoeShared.Blazor.Wpf;

public interface IBlazorWindowViewController : IWindowViewController
{
    ReactiveMetroWindowBase Window { get; }
    
    IBlazorWindow BlazorWindow { get; }
    
    void EnsureCreated();
}