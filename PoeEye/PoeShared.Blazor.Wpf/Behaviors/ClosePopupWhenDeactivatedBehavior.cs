using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Interop;
using Microsoft.VisualBasic.Logging;
using Microsoft.Web.WebView2.Wpf;
using PInvoke;
using PoeShared.Blazor.Wpf;

namespace PoeShared.Scaffolding.WPF;

public sealed class ClosePopupWhenDeactivatedBehavior : Behavior<Popup>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.Opened += AssociatedObjectOnOpened;
    }

    private void AssociatedObjectOnOpened(object sender, EventArgs e)
    {
        UIElement focusable = null;
        foreach (var element in AssociatedObject.Child.VisualDescendants().OfType<UIElement>())
        {
            if (!element.Focusable)
            {
                continue;
            }

            if (element is WebView2 webView)
            {
                focusable = webView;
                break;
            }

            if (focusable == null)
            {
                focusable = element;
            }
        }
        focusable?.Focus();

        var anchors = new CompositeDisposable();
        Disposable.Create(() => { }).AddTo(anchors);
        
        Observable
            .FromEventPattern(h => AssociatedObject.Closed += h, h => AssociatedObject.Closed -= h)
            .Subscribe(x =>
            {
                anchors.Dispose();
            })
            .AddTo(anchors);

        var popupRoot = AssociatedObject.Child.FindVisualTreeRoot();
        var popupRootSource = (HwndSource)PresentationSource.FromDependencyObject(popupRoot);
        popupRootSource.RegisterWndProc(PopupWindowHook).AddTo(anchors);

        //detecting on popup window seems to be reliable enough
        //var parentWindow = AssociatedObject.FindVisualAncestor<Window>();
        //parentWindow?.RegisterWndProc(ParentWindowHook).AddTo(anchors);
    }

    private IntPtr PopupWindowHook(IntPtr hwnd, int msgRaw, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (handled || lParam == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        var msg = (User32.WindowMessage)msgRaw;
        switch (msg)
        {
            case User32.WindowMessage.WM_KILLFOCUS:
            {
                
                break;
            }
            case User32.WindowMessage.WM_SETFOCUS:
            {
                
                break;
            }
            case User32.WindowMessage.WM_ACTIVATE:
            {
                var reason = (WindowActivationReason) wParam;
                if (reason == WindowActivationReason.WA_INACTIVE)
                {
                    if (AssociatedObject.IsOpen)
                    {
                        AssociatedObject.IsOpen = false;
                    }
                }

                break;
            }
            case User32.WindowMessage.WM_ACTIVATEAPP:
            {
                if (wParam == IntPtr.Zero)
                {
                    if (AssociatedObject.IsOpen)
                    {
                        AssociatedObject.IsOpen = false;
                    }
                }
                break;
            }
        }

        return IntPtr.Zero;
    }
     
     private enum WindowActivationReason
     {
         WA_INACTIVE = 0,
         WA_ACTIVE = 1,
         WA_CLICKACTIVE = 2
     }
}