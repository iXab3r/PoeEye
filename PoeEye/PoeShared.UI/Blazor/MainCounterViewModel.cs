using PoeShared.Blazor;
using PoeShared.Scaffolding;

namespace PoeShared.UI.Blazor;

public class MainCounterViewModel : DisposableReactiveComponent
{
    public MainCounterViewModel()
    {
    }
    
    public int Count { get; set; }
}