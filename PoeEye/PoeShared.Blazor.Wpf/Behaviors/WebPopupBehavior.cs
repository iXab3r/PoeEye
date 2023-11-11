using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interactivity;
using System.Windows.Interop;
using Microsoft.Web.WebView2.Wpf;
using PInvoke;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;

namespace PoeShared.Blazor.Wpf.Behaviors;

public sealed class WebPopupBehavior : Behavior<Popup>
{
    public static readonly DependencyProperty CloseWhenDeactivatedProperty = DependencyProperty.Register(
        nameof(CloseWhenDeactivated), typeof(bool), typeof(WebPopupBehavior), new PropertyMetadata(default(bool)));

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.Opened += AssociatedObjectOnOpened;
    }

    public bool CloseWhenDeactivated
    {
        get => (bool) GetValue(CloseWhenDeactivatedProperty);
        set => SetValue(CloseWhenDeactivatedProperty, value);
    }

    private void AssociatedObjectOnOpened(object sender, EventArgs e)
    {
        if (!CloseWhenDeactivated)
        {
            return;
        }
        
        /*
         * There are quite a few issues with displaying WebView2 in popup:
         * air-space - https://github.com/MicrosoftEdge/WebView2Feedback/issues/286
         * hit-test - https://github.com/MicrosoftEdge/WebView2Feedback/issues/997
         * focus - https://github.com/MicrosoftEdge/WebView2Feedback/issues/2531
         *
         * All-in-all, this behavior is an attempt to counter some of them by managing focus
         * Note that for all this to have effect, usually StaysOpen must be set to True,
         * otherwise popup will use its own mechanism(which will not work) to close the popup
        */
        
        WebView2 focusableWebView = null;
        UIElement focusableElement = null;
        foreach (var element in AssociatedObject.Child.VisualDescendants().OfType<UIElement>())
        {
            if (focusableWebView != null && focusableElement != null)
            {
                break;
            }
            
            if (!element.Focusable)
            {
                continue;
            }

            if (focusableWebView == null && element is WebView2 webView)
            {
                focusableWebView = webView;
                continue;
            }

            if (focusableElement == null)
            {
                focusableElement = element;
            }
        }

        var anchors = new CompositeDisposable();
        Disposable.Create(() => { }).AddTo(anchors);
        
        Observable
            .FromEventPattern(h => AssociatedObject.Closed += h, h => AssociatedObject.Closed -= h)
            .Subscribe(x =>
            {
                anchors.Dispose();
            })
            .AddTo(anchors);

        //var parentWindow = AssociatedObject.FindVisualAncestor<Window>();
        var popupRoot = AssociatedObject.Child.FindVisualTreeRoot();
        var popupRootSource = (HwndSource)PresentationSource.FromDependencyObject(popupRoot);
        if (popupRootSource == null)
        {
            return;
        }

        // there were multiple attempts, seems that finding out WebView
        var elementToFocus = focusableWebView ?? focusableElement;
        elementToFocus?.Focus();
        
        // this part will track deactivation and close popup if needed
        popupRootSource.RegisterWndProc(PopupWindowHook).AddTo(anchors);
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