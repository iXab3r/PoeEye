namespace PoeEye.PoeTrade.ViewModels
{
    using System;
    using System.Reactive.Concurrency;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    using Exceptionless;

    using Guards;

    using JetBrains.Annotations;

    using MetroModels;

    using Microsoft.Practices.Unity;

    using Models;

    using PoeEye.Prism;

    using PoeShared;
    using PoeShared.Scaffolding;

    using ReactiveUI;

    using WpfAwesomium;

    internal sealed class PoeTradeCaptchaViewModel : DisposableReactiveObject, IPoeTradeCaptchaViewModel
    {
        private static readonly TimeSpan RequestsThrottlePeriod = TimeSpan.FromSeconds(10);
        private readonly SerialDisposable browserSubscriptions = new SerialDisposable();

        private readonly IClock clock;
        private readonly IWindowTracker mainWindowTracker;
        private readonly IAudioNotificationsManager notificationsManager;

        private WpfChromium browser;

        private string captchaUri;

        private bool isBusy;

        private bool isOpen;

        private DateTime lastRequestTimestamp;

        public PoeTradeCaptchaViewModel(
            [NotNull] IDialogCoordinator dialogCoordinator,
            [NotNull] IClock clock,
            [NotNull] IPoeCaptchaRegistrator captchaRegistrator,
            [NotNull] IAudioNotificationsManager notificationsManager,
            [NotNull] [Dependency(WellKnownWindows.Main)] IWindowTracker mainWindowTracker,
            [NotNull] [Dependency(WellKnownSchedulers.Ui)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(() => dialogCoordinator);
            Guard.ArgumentNotNull(() => clock);
            Guard.ArgumentNotNull(() => mainWindowTracker);
            Guard.ArgumentNotNull(() => captchaRegistrator);
            Guard.ArgumentNotNull(() => notificationsManager);
            Guard.ArgumentNotNull(() => uiScheduler);

            this.clock = clock;
            this.notificationsManager = notificationsManager;
            this.mainWindowTracker = mainWindowTracker;

            captchaRegistrator
                .CaptchaRequests
                .Where(x => browser != null)
                .Where(x => !IsOpen)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Where(x => clock.Now - lastRequestTimestamp > RequestsThrottlePeriod)
                .Do(_ => ExceptionlessClient.Default.CreateEvent().SetMessage("Encountered CAPTCHA").Submit())
                .Subscribe(HandleRequest)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.Browser)
                .Subscribe(HandleBrowserChange)
                .AddTo(Anchors);

            browserSubscriptions.AddTo(Anchors);
        }

        public bool IsBusy
        {
            get { return isBusy; }
            set { this.RaiseAndSetIfChanged(ref isBusy, value); }
        }

        public WpfChromium Browser
        {
            get { return browser; }
            set { this.RaiseAndSetIfChanged(ref browser, value); }
        }

        public bool IsOpen
        {
            get { return isOpen; }
            set { this.RaiseAndSetIfChanged(ref isOpen, value); }
        }

        public string CaptchaUri
        {
            get { return captchaUri; }
            set { this.RaiseAndSetIfChanged(ref captchaUri, value); }
        }

        private void HandleRequest(string uri)
        {
            IsOpen = true;
            CaptchaUri = uri;
            lastRequestTimestamp = clock.Now;

            if (!mainWindowTracker.IsActive)
            {
                notificationsManager.PlayNotification(AudioNotificationType.Captcha);
            }

            browser.Source = new Uri(uri, UriKind.RelativeOrAbsolute);
        }

        private void HandleBrowserChange(WpfChromium browser)
        {
            if (browser == null)
            {
                browserSubscriptions.Disposable = null;
                return;
            }

            var composite = new CompositeDisposable();

            Observable
                .FromEventPattern<EventHandler<DocumentLoadedEventArgs>, DocumentLoadedEventArgs>(h => browser.DocumentLoaded += h, h => browser.DocumentLoaded -= h)
                .Select(x => new {x.EventArgs.Uri, Html = x.EventArgs.Value ?? string.Empty})
                .DistinctUntilChanged()
                .Subscribe(x => ProcessDocumentLoaded(x.Uri, x.Html))
                .AddTo(composite);

            browserSubscriptions.Disposable = composite;
        }

        private void ProcessDocumentLoaded(Uri uri, string html)
        {
            Log.Instance.Debug($"[PoeTradeCaptchaViewModel.Complete] Loaded uri {uri}, doc.Length: {html.Length}");
            if (string.IsNullOrWhiteSpace(html))
            {
                return;
            }

            if (html.Contains("Bad Request"))
            {
                Log.Instance.Warn($"[PoeTradeCaptchaViewModel.Complete] Detected bad request ! Closing panel...");
                IsOpen = false;
            }
        }
    }
}