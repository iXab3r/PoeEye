using PoeShared.Blazor;
using PoeShared.Blazor.Services;
using PoeShared.Scaffolding;

namespace PoeShared.UI.Blazor;

public class MainCounterViewModel : DisposableReactiveObject
{
    public MainCounterViewModel()
    {
    }
    
    public int Count { get; set; }
}