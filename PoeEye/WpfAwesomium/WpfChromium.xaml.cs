namespace WpfAwesomium
{
    using System;
    using System.ComponentModel;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Runtime.CompilerServices;
    using System.Windows;

    using Awesomium.Core;
    using Awesomium.Windows.Forms;

    using PoeShared.Scaffolding;

    using ReactiveUI;

    /// <summary>
    ///     Interaction logic for WpfChromium.xaml
    /// </summary>
    public partial class WpfChromium : INotifyPropertyChanged
    {
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            "Source",
            typeof (Uri),
            typeof (WpfChromium),
            new PropertyMetadata(default(Uri)));

        public static readonly DependencyProperty IsBusyProperty = DependencyProperty.Register(
            "IsBusy",
            typeof (bool),
            typeof (WpfChromium),
            new PropertyMetadata(default(bool)));

        private readonly SerialDisposable browserSubscriptions = new SerialDisposable();

        private WebControl browser;

        static WpfChromium()
        {
            var webConfig = WebConfig.Default;
            webConfig.LogLevel = LogLevel.None;
            WebCore.Initialize(webConfig);

            Application.Current.Exit += CurrentOnExit;
        }

        public WpfChromium()
        {
            InitializeComponent();

            this.WhenAnyValue(x => x.Browser)
                .Subscribe(HandleBrowserChange);
        }

        public bool IsBusy
        {
            get { return (bool) GetValue(IsBusyProperty); }
            set { SetValue(IsBusyProperty, value); }
        }

        public WebControl Browser
        {
            get { return browser; }
            set
            {
                browser = value;
                OnPropertyChanged(nameof(Browser));
            }
        }

        public Uri Source
        {
            get { return (Uri) GetValue(SourceProperty); }
            set
            {
                SetValue(SourceProperty, value);
                OnPropertyChanged(nameof(Source));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private static void CurrentOnExit(object sender, ExitEventArgs exitEventArgs)
        {
            WebCore.Shutdown();
        }

        public event EventHandler<DocumentLoadedEventArgs> DocumentLoaded = delegate { };

        private void HandleBrowserChange(WebControl browser)
        {
            if (browser == null)
            {
                browserSubscriptions.Disposable = null;
                return;
            }

            var composite = new CompositeDisposable();

            this.WhenAnyValue(x => x.Source)
                .Subscribe(x => browser.Source = x)
                .AddTo(composite);

            browser.WhenAnyValue(x => x.IsNavigating)
                   .Subscribe(isNavigating => IsBusy = isNavigating)
                   .AddTo(composite);

            Observable
                .FromEventPattern<DocumentReadyEventHandler, DocumentReadyEventArgs>(h => browser.DocumentReady += h, h => browser.DocumentReady -= h)
                .Select(x => new {x.EventArgs.Url, Html = browser.HTML ?? string.Empty})
                .DistinctUntilChanged()
                .Subscribe(
                    x => DocumentLoaded(
                        this,
                        new DocumentLoadedEventArgs
                        {
                            Uri = x.Url,
                            Value = x.Html
                        }))
                .AddTo(composite);

            browserSubscriptions.Disposable = composite;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}