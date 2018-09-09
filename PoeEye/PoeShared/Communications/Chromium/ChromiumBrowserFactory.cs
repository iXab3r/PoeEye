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
        private static readonly string AssemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public ChromiumBrowserFactory(
            [NotNull] IFactory<ChromiumBrowser, ChromiumWebBrowser> browserFactory)
        {
            Guard.ArgumentNotNull(browserFactory, nameof(browserFactory));
            this.browserFactory = browserFactory;

            Log.Instance.Debug("[ChromiumBrowserFactory] Initializing CEF...");
            if (Cef.IsInitialized)
            {
                throw new ApplicationException("CEF is already initialized");
            }

            var settings = new CefSettings();
            settings.DisableGpuAcceleration();

            settings.MultiThreadedMessageLoop = true;
            settings.ExternalMessagePump = false;
            settings.CachePath = "cefcache";
            settings.SetOffScreenRenderingBestPerformanceArgs();
            settings.WindowlessRenderingEnabled = true;
            if (Log.Instance.IsTraceEnabled)
            {
                settings.LogSeverity = LogSeverity.Verbose;
            }
            else if (Log.Instance.IsDebugEnabled)
            {
                settings.LogSeverity = LogSeverity.Error;
            }
            else 
            {
                settings.LogSeverity = LogSeverity.Disable;
            }
            settings.IgnoreCertificateErrors = true;
            settings.CefCommandLineArgs.Add("no-proxy-server", "1");
            settings.UserAgent = "CefSharp Browser" + Cef.CefSharpVersion;
            settings.CefCommandLineArgs.Add("disable-extensions", "1");
            settings.CefCommandLineArgs.Add("disable-pdf-extension", "1");
            
            settings.BrowserSubprocessPath = Path.Combine(AssemblyDir,
                Environment.Is64BitProcess ? "x64" : "x86",
                "CefSharp.BrowserSubprocess.exe");

            Log.Instance.Debug($"[ChromiumBrowserFactory] CEF settings: {settings.DumpToTextRaw()}");
            if (!Cef.Initialize(settings, true, new BrowserProcessHandler()))
            {
                throw new ApplicationException("Failed to initialize CEF");
            }

            Disposable.Create(
                () =>
                {
                    Log.Instance.Debug("[ChromiumBrowserFactory] Shutting down CEF...");
                    Cef.Shutdown();
                    Log.Instance.Debug("[ChromiumBrowserFactory] CEF has been shut down");
                }).AddTo(Anchors);
        }

        public IChromiumBrowser CreateBrowser()
        {
            Log.Instance.Debug("[ChromiumBrowserFactory] Create new browser instance...");

            var isInitialized = new ManualResetEvent(false);

            var browserSettings = new BrowserSettings()
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
    