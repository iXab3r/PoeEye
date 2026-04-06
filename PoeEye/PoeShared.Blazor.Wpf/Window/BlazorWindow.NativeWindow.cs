using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shell;
using PoeShared.Blazor.Wpf.Automation;
using PoeShared.Logging;
using PoeShared.Native;
using PoeShared.Scaffolding;
using ReactiveUI;
using Unity;

namespace PoeShared.Blazor.Wpf;

internal partial class BlazorWindow
{
    private sealed class NativeWindow : ReactiveWindow
    {
        private readonly BlazorWindow owner;
        private readonly DockPanel rootPanel;
        private readonly Border bodyHost;
        private readonly Border titleBarHost;
        private readonly ContentControl titleBarContentHost;
        private readonly SerialDisposable bodyAutomationRegistrationAnchor;
        private readonly SerialDisposable titleBarAutomationRegistrationAnchor;

        public NativeWindow(BlazorWindow owner)
        {
            this.owner = owner;
            BodyContentControl = CreateBlazorContentControl(typeof(BlazorWindowContent));
            TitleBarContentControl = CreateBlazorContentControl(typeof(BlazorWindowContentHeader));
            bodyAutomationRegistrationAnchor = new SerialDisposable().AddTo(Anchors);
            titleBarAutomationRegistrationAnchor = new SerialDisposable().AddTo(Anchors);
            InitializeContainerBinding();
            InitializeAutomationBinding();
            (rootPanel, titleBarHost, titleBarContentHost, bodyHost) = CreateLayout();
            InitializeNativeChromeBinding();
            Content = rootPanel;
            UpdateTitleBarDisplayMode(owner.TitleBarDisplayMode);

            Anchors.Add(() => 
            {
                owner.Log.Debug("Disposed native window");
                Container = null;
                ChildContainer = null;
                BodyContentControl.Container = null;
                TitleBarContentControl.Container = null;
            });
        }

        public BlazorContentControl BodyContentControl { get; }

        public BlazorContentControl TitleBarContentControl { get; }

        public BlazorContentControl ContentControl => BodyContentControl;

        public IUnityContainer Container { get; set; }
        
        public IUnityContainer ChildContainer { get; private set; }
        
        public IFluentLog Log => owner.Log;

        private void InitializeContainerBinding()
        {
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

            this.WhenAnyValue(x => x.ChildContainer)
                .Subscribe(container =>
                {
                    BodyContentControl.Container = container;
                    TitleBarContentControl.Container = container;
                })
                .AddTo(Anchors);
        }

        private void InitializeNativeChromeBinding()
        {
            Observable
                .FromEventPattern<EventHandler, EventArgs>(h => SourceInitialized += h, h => SourceInitialized -= h)
                .Subscribe(_ => ApplyNativeSystemChrome())
                .AddTo(Anchors);
        }

        private void InitializeAutomationBinding()
        {
            Observable.CombineLatest(
                    this.WhenAnyValue(x => x.ChildContainer),
                    owner.WhenAnyValue(x => x.AutomationId),
                    (container, automationId) => new
                    {
                        Container = container,
                        AutomationId = automationId?.Trim() ?? string.Empty
                    })
                .SubscribeSafe(x => UpdateAutomationRegistration(x.Container, x.AutomationId), Log.HandleUiException)
                .AddTo(Anchors);
        }

        private void UpdateAutomationRegistration(IUnityContainer container, string automationId)
        {
            bodyAutomationRegistrationAnchor.Disposable = Disposable.Empty;
            titleBarAutomationRegistrationAnchor.Disposable = Disposable.Empty;

            if (string.IsNullOrWhiteSpace(automationId) || container == null)
            {
                return;
            }

            if (!container.IsRegistered<IBlazorWindowViewRegistryRegistrar>())
            {
                return;
            }

            var registrar = container.Resolve<IBlazorWindowViewRegistryRegistrar>();
            bodyAutomationRegistrationAnchor.Disposable = registrar.Register(new BlazorWindowViewHandle(BodyContentControl, automationId, BlazorWindowViewRole.Body));
            titleBarAutomationRegistrationAnchor.Disposable = registrar.Register(new BlazorWindowViewHandle(TitleBarContentControl, automationId, BlazorWindowViewRole.TitleBar));
        }

        private (DockPanel RootPanel, Border TitleBarHost, ContentControl TitleBarContentHost, Border BodyHost) CreateLayout()
        {
            var root = new DockPanel()
            {
                Background = Brushes.Transparent
            };

            var titleBar = new Border()
            {
                Visibility = Visibility.Collapsed,
                Background = Brushes.Transparent,
                Height = 0
            };

            var titleBarContent = new ContentControl()
            {
                Background = Brushes.Transparent,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };
            titleBar.Child = titleBarContent;

            root.Children.Add(titleBar);
            DockPanel.SetDock(titleBar, Dock.Top);

            var body = new Border()
            {
                Background = Brushes.Transparent,
                Child = BodyContentControl
            };
            root.Children.Add(body);

            return (root, titleBar, titleBarContent, body);
        }

        public void UpdateTitleBarDisplayMode(TitleBarDisplayMode titleBarDisplayMode)
        {
            var displayMode = titleBarDisplayMode.ResolveForWpf();

            switch (displayMode)
            {
                case TitleBarDisplayMode.System:
                    ApplySystemMode();
                    break;
                case TitleBarDisplayMode.Custom:
                    ApplyFramelessMode(TitleBarDisplayMode.Custom);
                    break;
                case TitleBarDisplayMode.None:
                    ApplyFramelessMode(TitleBarDisplayMode.None);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(titleBarDisplayMode), titleBarDisplayMode, null);
            }
        }

        private void ApplyFramelessMode(TitleBarDisplayMode displayMode)
        {
            TrySetAllowsTransparency(owner.AllowsTransparency, displayMode);
            WindowStyle = WindowStyle.None;
            WindowChrome.SetWindowChrome(this, null);
            titleBarContentHost.Content = null;
            titleBarHost.Visibility = Visibility.Collapsed;
            titleBarHost.Height = 0;
            titleBarHost.Margin = default;
            bodyHost.Margin = default;
        }

        private void ApplySystemMode()
        {
            if (owner.AllowsTransparency)
            {
                owner.Log.Warn($"{TitleBarDisplayMode.System} mode is not supported when {nameof(AllowsTransparency)} is enabled");
                return;
            }

            if (!TrySetAllowsTransparency(false, TitleBarDisplayMode.System))
            {
                owner.Log.Warn("System title bar mode requires a non-transparent window before the native handle is created; the request is ignored for this window instance");
                return;
            }

            var resizeBorderThickness = GetResizeBorderThickness(owner.BorderThickness, owner.ResizeMode);

            WindowStyle = WindowStyle.None;
            // Keep the same body presenter alive; only the title host moves between Blazor and native WPF.
            titleBarContentHost.Content = TitleBarContentControl;
            titleBarHost.Visibility = Visibility.Visible;
            titleBarHost.Height = GetTitleBarHeight();
            ApplySystemContentMargins(resizeBorderThickness);
            UpdateTitleBarInteractivity();

            WindowChrome.SetWindowChrome(this, new WindowChrome()
            {
                CaptionHeight = GetTitleBarHeight(),
                CornerRadius = new CornerRadius(0),
                GlassFrameThickness = GetGlassFrameThickness(),
                NonClientFrameEdges = NonClientFrameEdges.None,
                ResizeBorderThickness = resizeBorderThickness,
                UseAeroCaptionButtons = false
            });
            ApplyNativeSystemChrome();
        }

        private static Thickness GetResizeBorderThickness(Thickness borderThickness, ResizeMode resizeMode)
        {
            if (resizeMode is not ResizeMode.CanResize and not ResizeMode.CanResizeWithGrip)
            {
                return new Thickness(0);
            }

            return new Thickness(
                left: Math.Max(borderThickness.Left, SystemParameters.ResizeFrameVerticalBorderWidth),
                top: Math.Max(borderThickness.Top, SystemParameters.ResizeFrameHorizontalBorderHeight),
                right: Math.Max(borderThickness.Right, SystemParameters.ResizeFrameVerticalBorderWidth),
                bottom: Math.Max(borderThickness.Bottom, SystemParameters.ResizeFrameHorizontalBorderHeight));
        }

        private static double GetTitleBarHeight()
        {
            return Math.Max(SystemParameters.CaptionHeight, 32d);
        }

        private static Thickness GetGlassFrameThickness()
        {
            return new Thickness(1);
        }

        private void ApplySystemContentMargins(Thickness resizeBorderThickness)
        {
            titleBarHost.Margin = new Thickness(
                left: resizeBorderThickness.Left,
                top: resizeBorderThickness.Top,
                right: resizeBorderThickness.Right,
                bottom: 0);

            bodyHost.Margin = new Thickness(
                left: resizeBorderThickness.Left,
                top: 0,
                right: resizeBorderThickness.Right,
                bottom: resizeBorderThickness.Bottom);
        }

        private void UpdateTitleBarInteractivity()
        {
            if (titleBarContentHost == null)
            {
                return;
            }
            WindowChrome.SetIsHitTestVisibleInChrome(titleBarContentHost, titleBarHost.Visibility == Visibility.Visible);
        }

        private void ApplyNativeSystemChrome()
        {
            var displayMode = owner.TitleBarDisplayMode.ResolveForWpf();
            if (displayMode != TitleBarDisplayMode.System || WindowHandle == IntPtr.Zero)
            {
                return;
            }

            var canResize = owner.ResizeMode is ResizeMode.CanResize or ResizeMode.CanResizeWithGrip;
            UnsafeNative.ConfigureCustomWindowFrame(WindowHandle, canResize);
            UnsafeNative.TryEnableRoundedCorners(WindowHandle);
        }

        public void UpdateAllowsTransparency(bool allowsTransparency)
        {
            if (owner.TitleBarDisplayMode.ResolveForWpf() == TitleBarDisplayMode.System && allowsTransparency)
            {
                owner.Log.Warn($"{nameof(AllowsTransparency)} cannot be enabled while {nameof(TitleBarDisplayMode)} is {TitleBarDisplayMode.System}");
                return;
            }

            TrySetAllowsTransparency(allowsTransparency, owner.TitleBarDisplayMode.ResolveForWpf());
        }

        private bool TrySetAllowsTransparency(bool allowsTransparency, TitleBarDisplayMode displayMode)
        {
            if (AllowsTransparency == allowsTransparency)
            {
                return true;
            }

            if (WindowHandle != IntPtr.Zero)
            {
                owner.Log.Warn($"Cannot switch {nameof(AllowsTransparency)} to {allowsTransparency} for {displayMode} mode after the native window handle is created");
                return false;
            }

            AllowsTransparency = allowsTransparency;
            return true;
        }

        private BlazorContentControl CreateBlazorContentControl(Type viewType)
        {
            return new BlazorContentControl()
            {
                ViewType = viewType,
                AdditionalFileProvider = owner.compositeFileProvider,
                Configurator = owner.ControlConfigurator,
                Content = owner
            }.AddTo(Anchors);
        }
    }
}
