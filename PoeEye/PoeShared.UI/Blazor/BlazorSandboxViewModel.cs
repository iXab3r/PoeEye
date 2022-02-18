using DynamicData;
using PoeShared.Blazor.Wpf;
using PoeShared.Scaffolding;

namespace PoeShared.UI.Blazor;

public sealed class BlazorSandboxViewModel : DisposableReactiveObject
{
    public BlazorHostViewModel WebViewHost { get; }

    public BlazorSandboxViewModel(
        BlazorHostViewModel host)
    {
        WebViewHost = host.AddTo(Anchors);
        WebViewHost.Components.Add(typeof(MainCounter));
    }
}