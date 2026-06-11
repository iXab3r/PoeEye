using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using PoeShared.Blazor.Wpf.Automation;
using PoeShared.Scaffolding;
using ReactiveUI;
using Unity;

namespace PoeShared.Blazor.Wpf;

internal partial class BlazorWindow
{
    /// <summary>
    /// Blazor-hosting inner window: body and title bar are <see cref="BlazorContentControl"/>s (WebView2),
    /// wired to the per-window child Unity container and the automation view registry.
    /// </summary>
    private sealed class BlazorWindowView : WindowView
    {
        private readonly BlazorWindow owner;
        private readonly SerialDisposable bodyAutomationRegistrationAnchor;
        private readonly SerialDisposable titleBarAutomationRegistrationAnchor;

        public BlazorWindowView(BlazorWindow owner) : base(owner)
        {
            this.owner = owner;
            BodyContentControl = CreateBlazorContentControl(typeof(BlazorWindowContent));
            TitleBarContentControl = CreateBlazorContentControl(typeof(BlazorWindowContentHeader));
            bodyAutomationRegistrationAnchor = new SerialDisposable().AddTo(Anchors);
            titleBarAutomationRegistrationAnchor = new SerialDisposable().AddTo(Anchors);
            InitializeContainerBinding();
            InitializeAutomationBinding();
            BodyHost.Child = BodyContentControl;
            ApplyInitialTitleBarDisplayMode();

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

        public override BlazorContentControl ContentControl => BodyContentControl;

        public IUnityContainer Container { get; set; }

        public IUnityContainer ChildContainer { get; private set; }

        public override void UpdateHostedContent(Func<INativeWindow, UIElement> contentFactory)
        {
            throw new NotSupportedException($"{nameof(BlazorWindow)} hosts Blazor content - assign {nameof(ViewType)} instead of {nameof(ContentFactory)}");
        }

        private protected override object GetTitleBarContent()
        {
            return TitleBarContentControl;
        }

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
