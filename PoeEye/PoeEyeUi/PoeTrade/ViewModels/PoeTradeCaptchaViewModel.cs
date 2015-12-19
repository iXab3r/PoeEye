namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Reactive.Concurrency;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Windows.Controls;
    using System.Windows.Media.Animation;

    using Awesomium.Core;
    using Awesomium.Windows.Forms;

    using Guards;

    using JetBrains.Annotations;

    using MetroModels;

    using Microsoft.Practices.Unity;

    using Models;

    using NuGet;

    using PoeShared;
    using PoeShared.Utilities;

    using Prism;

    using ReactiveUI;

    internal sealed class PoeTradeCaptchaViewModel : DisposableReactiveObject, IPoeTradeCaptchaViewModel
    {
        private static readonly TimeSpan RequestsThrottlePeriod = TimeSpan.FromSeconds(10);

        private readonly IClock clock;
        private readonly IAudioNotificationsManager notificationsManager;
        private readonly SerialDisposable browserSubscriptions = new SerialDisposable();

        private DateTime lastRequestTimestamp;

        public PoeTradeCaptchaViewModel(
                [NotNull] IDialogCoordinator dialogCoordinator,
                [NotNull] IClock clock,
                [NotNull] IPoeCaptchaRegistrator captchaRegistrator,
                [NotNull] IAudioNotificationsManager notificationsManager,
                [NotNull] [Dependency(WellKnownSchedulers.Ui)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(() => dialogCoordinator);
            Guard.ArgumentNotNull(() => clock);
            Guard.ArgumentNotNull(() => captchaRegistrator);
            Guard.ArgumentNotNull(() => notificationsManager);
            Guard.ArgumentNotNull(() => uiScheduler);

            this.clock = clock;
            this.notificationsManager = notificationsManager;

            captchaRegistrator
                .CaptchaRequests
                .Where(x => !IsOpen)
                .Where(x => browser != null)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Where(x => clock.CurrentTime - lastRequestTimestamp > RequestsThrottlePeriod)
                .Subscribe(HandleRequest)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.Browser)
                .Subscribe(HandleBrowserChange)
                .AddTo(Anchors);

            browserSubscriptions.AddTo(Anchors);

            var webConfig = WebConfig.Default;
            webConfig.LogLevel = LogLevel.None;
            WebCore.Initialize(webConfig);
            Anchors.Add(new DisposableAction(DisposeBrowser));
        }

        private bool isOpen;

        public bool IsOpen
        {
            get { return isOpen; }
            set { this.RaiseAndSetIfChanged(ref isOpen, value); }
        }

        private string captchaUri;

        public string CaptchaUri
        {
            get { return captchaUri; }
            set { this.RaiseAndSetIfChanged(ref captchaUri, value); }
        }

        private WebControl browser;

        public WebControl Browser
        {
            get { return browser; }
            set { this.RaiseAndSetIfChanged(ref browser, value); }
        }

        private bool isBusy;

        public bool IsBusy
        {
            get { return isBusy; }
            set { this.RaiseAndSetIfChanged(ref isBusy, value); }
        }

        private void HandleRequest(string uri)
        {
            IsOpen = true;
            CaptchaUri = uri;
            lastRequestTimestamp = clock.CurrentTime;

            notificationsManager.PlayNotificationCommand.Execute(AudioNotificationType.Captcha);
            browser.Source = new Uri(uri, UriKind.RelativeOrAbsolute);
        }

        private void HandleBrowserChange(WebControl browser)
        {
            if (browser == null)
            {
                browserSubscriptions.Disposable = null;
                return;
            }

            var composite = new CompositeDisposable();

            browser.WhenAnyValue(x => x.IsNavigating)
                   .Subscribe(isNavigating => IsBusy = isNavigating)
                   .AddTo(composite);

            browser.WhenAnyValue(x => x.IsNavigating)
                   .Subscribe(isNavigating => browser.Visible = !isNavigating)
                   .AddTo(composite);

            Observable
                .FromEventPattern<LoadingFrameFailedEventHandler, LoadingFrameFailedEventArgs>(h => browser.LoadingFrameFailed += h, h => browser.LoadingFrameFailed -= h)
                .Subscribe(x => Log.Instance.Warn($"[PoeTradeCaptchaViewModel.Error] Failed to load uri {x.EventArgs.Url}, errorCode; {x.EventArgs.ErrorCode}, desc: {x.EventArgs.ErrorCode}"))
                .AddTo(composite);

            Observable
               .FromEventPattern<DocumentReadyEventHandler, DocumentReadyEventArgs>(h => browser.DocumentReady += h, h => browser.DocumentReady -= h)
               .Select(x => new { x.EventArgs.Url, Html = browser.HTML ?? string.Empty })
               .DistinctUntilChanged()
               .Subscribe(x => ProcessDocumentLoaded(x.Url, x.Html))
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

        private void DisposeBrowser()
        {
            Log.Instance.Debug($"[PoeTradeCaptchaViewModel.Dispose] Diposing web browser({browser})");
            browser?.Dispose();
            Log.Instance.Debug($"[PoeTradeCaptchaViewModel.Dispose] Shutting down core...");
            WebCore.Shutdown();
        }
    }
}