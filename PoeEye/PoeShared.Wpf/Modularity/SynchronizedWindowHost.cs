using System;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using PoeShared.Native;
using PoeShared.Scaffolding;
using PoeShared.UI;
using ReactiveUI;

namespace PoeShared.Modularity;

public sealed class SynchronizedWindowHost : Border
{
    private ReactiveMetroWindow parentWindow;

    public SynchronizedWindowHost()
    {
        this.Loaded += ParentOnLoaded;
    }

    private void ParentOnLoaded(object sender, RoutedEventArgs e)
    {
        ParentWindow = this.FindVisualAncestor<ReactiveMetroWindow>();
        ParentContainer = this.FindVisualAncestor<ContentPresenter>();

        ParentWindow.LocationChanged += ParentWindowOnLocationChanged;


    }

    private void ParentWindowOnLocationChanged(object sender, EventArgs e)
    {
        var newSize = ParentWindow.RenderSize;
        
    }

    public ReactiveMetroWindow ChildWindow { get; set; }
    public ReactiveMetroWindow ParentWindow { get; private set; }
    public UIElement ParentContainer { get; private set; }

    private void RunAction(ReactiveMetroWindow window, Action<ReactiveMetroWindow> action)
    {
        if (!window.Dispatcher.CheckAccess())
        {
            window.Dispatcher.Invoke(() => RunAction(window, action));
            return;
        }

        action(window);
    }
}