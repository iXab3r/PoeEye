using System;
using System.Reactive.Linq;
using MahApps.Metro.Controls;
using PoeShared.Scaffolding;
using PoeShared.UI;
using ReactiveUI;
using Unity;

namespace PoeShared.Blazor.Wpf;

internal partial class BlazorWindow
{
    private sealed class NativeWindow : ReactiveMetroWindowBase
    {
        public NativeWindow(BlazorWindow owner)
        {
            this.WhenAnyValue(x => x.Container)
                .Select(x => x ?? owner.unityContainer)
                .Subscribe(parentContainer =>
                {
                    var childContainer = parentContainer.CreateChildContainer().AddTo(Anchors);
                    childContainer.RegisterSingleton<IBlazorWindowController>(_ => owner);
                    ChildContainer = childContainer;
                })
                .AddTo(Anchors);

            ContentControl = new BlazorContentControl()
            {
                Container = ChildContainer,
                ViewType = typeof(BlazorWindowContent),
                AdditionalFileProvider = owner.complexFileProvider,
                Content = owner
            }.AddTo(Anchors);
            this.WhenAnyValue(x => x.ChildContainer)
                .Subscribe(x => { ContentControl.Container = x; })
                .AddTo(Anchors);

            Content = ContentControl;
            AllowsTransparency = true;
            WindowButtonCommandsOverlayBehavior = OverlayBehavior.Never;
            LeftWindowCommandsOverlayBehavior = WindowCommandsOverlayBehavior.Never;
            RightWindowCommandsOverlayBehavior = WindowCommandsOverlayBehavior.Never;

            Anchors.Add(() => Log.Debug("Disposed native window"));
        }

        public BlazorContentControl ContentControl { get; }

        public IUnityContainer Container { get; set; }
        
        public IUnityContainer ChildContainer { get; private set; }
    }
}