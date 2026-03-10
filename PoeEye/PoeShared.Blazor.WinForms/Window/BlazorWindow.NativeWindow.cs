using System;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using PoeShared.Logging;
using PoeShared.Native;
using PoeShared.Scaffolding;
using ReactiveUI;
using Unity;
using Padding = System.Windows.Forms.Padding;
using PoeShared.Blazor.Wpf;

namespace PoeShared.Blazor.WinForms;

internal partial class BlazorWindow
{
    private sealed class NativeWindow : ReactiveForm
    {
        private readonly BlazorWindow owner;
        private readonly TableLayoutPanel rootPanel;
        private readonly Panel bodyHost;
        private readonly Panel titleBarHost;
        private readonly Panel titleBarContentHost;
        private IUnityContainer container;
        private IUnityContainer childContainer;

        public NativeWindow(BlazorWindow owner)
        {
            this.owner = owner;
            BodyContentControl = CreateBlazorContentHost(typeof(BlazorWindowContent));
            TitleBarContentControl = CreateBlazorContentHost(typeof(BlazorWindowContentHeader));
            BorderThickness = default;
            InitializeContainerBinding();
            (rootPanel, titleBarHost, titleBarContentHost, bodyHost) = CreateLayout();
            InitializeNativeChromeBinding();
            Controls.Add(rootPanel);
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

        public BlazorContentHost BodyContentControl { get; }

        public BlazorContentHost TitleBarContentControl { get; }

        public BlazorContentHost ContentControl => BodyContentControl;

        public Thickness BorderThickness { get; set; }

        public bool AllowsTransparency { get; private set; }

        public IUnityContainer Container
        {
            get => container;
            set
            {
                if (ReferenceEquals(container, value))
                {
                    return;
                }

                container = value;
                RaisePropertyChanged(nameof(Container));
            }
        }

        public IUnityContainer ChildContainer
        {
            get => childContainer;
            private set
            {
                if (ReferenceEquals(childContainer, value))
                {
                    return;
                }

                childContainer = value;
                RaisePropertyChanged(nameof(ChildContainer));
            }
        }

        public IFluentLog Log => owner.Log;

        public void ApplyResizeMode(ResizeMode resizeMode)
        {
            ResizeMode = resizeMode;
            MinimizeBox = resizeMode is ResizeMode.CanResize or ResizeMode.CanResizeWithGrip or ResizeMode.CanMinimize;
            MaximizeBox = resizeMode is ResizeMode.CanResize or ResizeMode.CanResizeWithGrip;
        }

        public void ApplyBackgroundColor(Color color)
        {
            var drawingColor = System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
            BackColor = drawingColor;
            rootPanel.BackColor = drawingColor;
            bodyHost.BackColor = drawingColor;
            titleBarHost.BackColor = drawingColor;
            titleBarContentHost.BackColor = drawingColor;
        }

        public void ApplyBorderColor(Color color)
        {
            var drawingColor = System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
            bodyHost.BackColor = drawingColor;
        }

        private void InitializeContainerBinding()
        {
            this.WhenAnyValue(x => x.Container)
                .Select(x => x ?? owner.unityContainer)
                .Subscribe(parentContainer =>
                {
                    var nextChildContainer = parentContainer.CreateChildContainer().AddTo(owner.Anchors);
                    nextChildContainer.RegisterSingleton<IBlazorWindowController>(_ => owner);
                    nextChildContainer.RegisterSingleton<IBlazorWindowAccessor>(_ => new BlazorWindowAccessor(owner));
                    ChildContainer = nextChildContainer;
                })
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.ChildContainer)
                .Subscribe(nextContainer =>
                {
                    BodyContentControl.Container = nextContainer;
                    TitleBarContentControl.Container = nextContainer;
                })
                .AddTo(Anchors);
        }

        private void InitializeNativeChromeBinding()
        {
            Observable
                .FromEventPattern<EventHandler, EventArgs>(h => HandleCreated += h, h => HandleCreated -= h)
                .Subscribe(_ => ApplyNativeSystemChrome())
                .AddTo(Anchors);
        }

        private (TableLayoutPanel RootPanel, Panel TitleBarHost, Panel TitleBarContentHost, Panel BodyHost) CreateLayout()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.Transparent,
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var titleBar = new Panel
            {
                Visible = false,
                Height = 0,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = System.Drawing.Color.Transparent,
            };

            var titleBarContent = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = System.Drawing.Color.Transparent,
            };
            titleBar.Controls.Add(titleBarContent);
            titleBarContent.Controls.Add(TitleBarContentControl);
            TitleBarContentControl.Dock = DockStyle.Fill;

            var body = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = System.Drawing.Color.Transparent,
            };
            body.Controls.Add(BodyContentControl);
            BodyContentControl.Dock = DockStyle.Fill;

            root.Controls.Add(titleBar, 0, 0);
            root.Controls.Add(body, 0, 1);

            return (root, titleBar, titleBarContent, body);
        }

        public void UpdateTitleBarDisplayMode(TitleBarDisplayMode titleBarDisplayMode)
        {
            var displayMode = titleBarDisplayMode == TitleBarDisplayMode.Default
                ? TitleBarDisplayMode.Custom
                : titleBarDisplayMode;

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

        public void ApplyWindowPadding(Thickness padding)
        {
            var translatedPadding = new Padding(
                Math.Max((int)padding.Left, 1),
                Math.Max((int)padding.Top, 1),
                Math.Max((int)padding.Right, 1),
                Math.Max((int)padding.Bottom, 1));
            bodyHost.Padding = translatedPadding;
        }

        private void ApplyFramelessMode(TitleBarDisplayMode displayMode)
        {
            TrySetAllowsTransparency(owner.AllowsTransparency, displayMode);
            FormBorderStyle = FormBorderStyle.None;
            ControlBox = false;
            titleBarContentHost.Visible = false;
            titleBarHost.Visible = false;
            titleBarHost.Height = 0;
            rootPanel.RowStyles[0].Height = 0;
            titleBarHost.Margin = Padding.Empty;
            bodyHost.Margin = Padding.Empty;
            BodyContentControl.ViewType = typeof(BlazorWindowContent);
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

            FormBorderStyle = GetSystemFormBorderStyle(owner.ResizeMode);
            BodyContentControl.ViewType = typeof(BlazorWindowContentBody);
            titleBarContentHost.Visible = false;
            titleBarHost.Visible = false;
            titleBarHost.Height = 0;
            rootPanel.RowStyles[0].Height = 0;
            titleBarHost.Margin = Padding.Empty;
            bodyHost.Margin = Padding.Empty;
            ApplySystemCaptionButtons();

            ApplyNativeSystemChrome();
        }

        private static FormBorderStyle GetSystemFormBorderStyle(ResizeMode resizeMode)
        {
            return resizeMode switch
            {
                ResizeMode.CanResize => FormBorderStyle.Sizable,
                ResizeMode.CanResizeWithGrip => FormBorderStyle.Sizable,
                ResizeMode.CanMinimize => FormBorderStyle.FixedSingle,
                ResizeMode.NoResize => FormBorderStyle.FixedSingle,
                _ => FormBorderStyle.FixedSingle
            };
        }

        private void ApplySystemCaptionButtons()
        {
            var canResize = owner.ResizeMode is ResizeMode.CanResize or ResizeMode.CanResizeWithGrip;
            var canMinimize = owner.ResizeMode is ResizeMode.CanResize or ResizeMode.CanResizeWithGrip or ResizeMode.CanMinimize;
            var showMinButton = owner.ShowMinButton && canMinimize;
            var showMaxButton = owner.ShowMaxButton && canResize;
            var showCloseButton = owner.ShowCloseButton;
            var hasSystemMenu = showCloseButton || showMinButton || showMaxButton;

            ControlBox = hasSystemMenu;
            MinimizeBox = showMinButton;
            MaximizeBox = showMaxButton;

            if (WindowHandle != IntPtr.Zero)
            {
                UnsafeNative.ConfigureSystemCaptionButtons(WindowHandle, showCloseButton, showMinButton, showMaxButton, canResize);
            }
        }

        private void ApplyNativeSystemChrome()
        {
            var displayMode = owner.TitleBarDisplayMode == TitleBarDisplayMode.Default
                ? TitleBarDisplayMode.Custom
                : owner.TitleBarDisplayMode;
            if (displayMode != TitleBarDisplayMode.System || WindowHandle == IntPtr.Zero)
            {
                return;
            }

            ApplySystemCaptionButtons();
        }

        public void UpdateAllowsTransparency(bool allowsTransparency)
        {
            if (owner.TitleBarDisplayMode == TitleBarDisplayMode.System && allowsTransparency)
            {
                owner.Log.Warn($"{nameof(AllowsTransparency)} cannot be enabled while {nameof(TitleBarDisplayMode)} is {TitleBarDisplayMode.System}");
                return;
            }

            TrySetAllowsTransparency(allowsTransparency, owner.TitleBarDisplayMode);
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

        private BlazorContentHost CreateBlazorContentHost(Type viewType)
        {
            return new BlazorContentHost
            {
                Dock = DockStyle.Fill,
                ViewType = viewType,
                AdditionalFileProvider = owner.compositeFileProvider,
                Configurator = owner.ControlConfigurator,
                Content = owner
            }.AddTo(Anchors);
        }
    }
}
