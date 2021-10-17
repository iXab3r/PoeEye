using PoeShared.Bindings;
using PoeShared.Scaffolding;

namespace PoeShared.UI.Bindings
{
    public interface IBindingsEditorViewModel : IDisposableReactiveObject
    {
        BindableReactiveObject Source { get; set; }
        
        DisposableReactiveObject ValueSource { get; set; }
    }
}