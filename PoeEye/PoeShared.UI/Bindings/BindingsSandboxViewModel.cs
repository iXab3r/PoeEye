using PoeShared.Scaffolding;

namespace PoeShared.UI.Bindings;

public class BindingsSandboxViewModel : DisposableReactiveObject
{
    public BindingsSandboxViewModel(IBindingsEditorViewModel editor)
    {
        Source = new StubViewModel();
        Target = new StubViewModel();
        BindingsEditor = editor;
        editor.Source = Target;
        editor.ValueSource = Source;
    }
        
    public IBindingsEditorViewModel BindingsEditor { get; }

    public StubViewModel Source { get; }
        
    public StubViewModel Target { get; }
}