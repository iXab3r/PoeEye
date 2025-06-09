using System;
using System.Reactive.Linq;
using System.Windows;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using ReactiveUI;
using Unity;

namespace PoeShared.Blazor.Wpf;

internal partial class BlazorWindow
{
    private sealed class NativeWindow : ReactiveWindow
    {
        private readonly BlazorWindow owner;

        public NativeWindow(BlazorWindow owner)
        {
            this.owner = owner;
            this.WhenAnyValue(x => x.Container)
                .Select(x => x ?? owner.unityContainer)
                .Subscribe(parentContainer =>
                {
                    //that is a very shady moment - child container has to be kept alive for the entire period
                    //kept alive = managed via Parent Anchors, not its own
                    //otherwise, when _current_ window gets disposed, the entire container will be disposed
                    //maybe move registrations to another nested container to avoid this?
                    var childContainer = parentContainer.CreateChildContainer().AddTo(owner.Anchors);
                    childContainer.RegisterSingleton<IBlazorWindowController>(_ => owner);
                    childContainer.RegisterSingleton<IBlazorWindowAccessor>(_ => new BlazorWindowAccessor(owner));
                    ChildContainer = childContainer;
                })
                .AddTo(Anchors);

            ContentControl = new BlazorContentControl()
            {
                ViewType = typeof(BlazorWindowContent),
                AdditionalFileProvider = owner.complexFileProvider,
                Content = owner
            }.AddTo(Anchors);
            
            this.WhenAnyValue(x => x.ChildContainer)
                .Subscribe(x => { ContentControl.Container = x; })
                .AddTo(Anchors);

            Content = ContentControl;
            AllowsTransparency = true;
            WindowStyle = WindowStyle.None; //this basically disables System/Default

            Anchors.Add(() => owner.Log.Debug("Disposed native window"));
        }

        public BlazorContentControl ContentControl { get; }

        public IUnityContainer Container { get; set; }
        
        public IUnityContainer ChildContainer { get; private set; }
        
        public IFluentLog Log => owner.Log;
    }
}