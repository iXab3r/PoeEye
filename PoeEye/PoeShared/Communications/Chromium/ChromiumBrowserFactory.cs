using System;
using System.IO;
using System.Reactive.Disposables;
using System.Reflection;
using System.Threading;
using CefSharp;
using CefSharp.OffScreen;
using Guards;
using JetBrains.Annotations;
using PoeShared.Prism;
using PoeShared.Scaffolding;

namespace PoeShared.Communications.Chromium
{
    internal sealed class ChromiumBrowserFactory : DisposableReactiveObject, IChromiumBrowserFactory
    {
        private readonly IFactory<ChromiumBrowser, ChromiumWebBrowser> browserFactory;

        public ChromiumBrowserFactory(
            [NotNull] IFactory<ChromiumBrowser, ChromiumWebBrowser> browserFactory)
        {
            Guard.ArgumentNotNull(browserFactory, nameof(browserFactory));
            this.browserFactory = browserFactory;
        }

        public IChromiumBrowser CreateBrowser()
        {
            Log.Instance.Debug("[ChromiumBrowserFactory] Create new browser instance...");

            var isInitialized = new ManualResetEvent(false);

            var browserSettings = new BrowserSettings
            {
                WindowlessFrameRate = 1,
                ImageLoading = CefState.Disabled,
                JavascriptCloseWindows = CefState.Disabled,
                JavascriptAccessClipboard = CefState.Disabled,
                JavascriptOpenWindows = CefState.Disabled,
                Javascript = CefState.Enabled
            };

            var contextSettings = new RequestContextSettings();

            var requestContext = new RequestContext(contextSettings).AddTo(Anchors);

            var browserInstance = new ChromiumWebBrowser("", browserSettings, requestContext, true);

            EventHandler browserInitialized = null;
            browserInitialized = (sender, args) =>
            {
                browserInstance.BrowserInitialized -= browserInitialized;
                isInitialized.Set();
            };
            browserInstance.BrowserInitialized += browserInitialized;

            Log.Instance.Debug("[ChromiumBrowserFactory] Awaiting for initialization to be completed");
            isInitialized.WaitOne();

            browserInstance.RequestHandler = new LogRequestHandler();
            browserInstance.RenderProcessMessageHandler = new RenderProcessMessageHandler();

            Log.Instance.Debug("[ChromiumBrowserFactory] New browser instance initialized");

            return browserFactory.Create(browserInstance).AddTo(Anchors);
        }
    }
}