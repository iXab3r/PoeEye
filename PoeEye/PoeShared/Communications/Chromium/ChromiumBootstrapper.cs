using System;
using System.IO;
using System.Reactive.Disposables;
using System.Reflection;
using System.Threading;
using System.Windows;
using CefSharp;
using CefSharp.OffScreen;
using Guards;
using JetBrains.Annotations;
using PoeShared.Prism;
using PoeShared.Scaffolding;

namespace PoeShared.Communications.Chromium
{
    internal sealed class ChromiumBootstrapper : DisposableReactiveObject, IChromiumBootstrapper
    {
        private readonly IFactory<ChromiumBrowser, ChromiumWebBrowser> browserFactory;
        private static readonly string AssemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public ChromiumBootstrapper(
            [NotNull] IFactory<ChromiumBrowser, ChromiumWebBrowser> browserFactory)
        {
            Guard.ArgumentNotNull(browserFactory, nameof(browserFactory));
            this.browserFactory = browserFactory;
            
            Log.Instance.Debug("[ChromiumBootstrapper] Initializing CEF...");
            if (Cef.IsInitialized)
            {
                throw new ApplicationException("CEF is already initialized");
            }
            
            var settings = new CefSettings();
            settings.CachePath = "cefcache";
            settings.SetOffScreenRenderingBestPerformanceArgs();
            settings.WindowlessRenderingEnabled = true;
            settings.LogSeverity = LogSeverity.Verbose;
            settings.IgnoreCertificateErrors = true;
            settings.CefCommandLineArgs.Add("no-proxy-server", "1");
            settings.UserAgent = "CefSharp Browser" + Cef.CefSharpVersion;
            settings.CefCommandLineArgs.Add("disable-extensions", "1");    
            settings.CefCommandLineArgs.Add("disable-pdf-extension", "1");

            Log.Instance.Debug($"[ChromiumBootstrapper] CEF settings: {settings.DumpToTextRaw()}");

            if (!Cef.Initialize(settings))
            {
                throw new ApplicationException("Failed to initialize CEF");
            }

            Disposable.Create(
                () =>
                {
                    Log.Instance.Debug("[ChromiumBootstrapper] Shutting down CEF...");
                    Cef.Shutdown();
                    Log.Instance.Debug("[ChromiumBootstrapper] CEF has been shut down");
                }).AddTo(Anchors);
        }
        
        public IPoeBrowser CreateBrowser()
        {
            Log.Instance.Debug("[ChromiumBootstrapper] Create new browser instance...");

            var isInitialized = new ManualResetEvent(false);
            var browserInstance = new ChromiumWebBrowser();

            EventHandler browserInitialized = null;
            browserInitialized = (sender, args) =>
            {
                browserInstance.BrowserInitialized -= browserInitialized;
                isInitialized.Set();
            };
            browserInstance.BrowserInitialized += browserInitialized;
            
            Log.Instance.Debug("[ChromiumBootstrapper] Awaiting for initialization to be completed");
            isInitialized.WaitOne();
            
            Log.Instance.Debug("[ChromiumBootstrapper] New browser instance initialized");
            return browserFactory.Create(browserInstance).AddTo(Anchors);
        }
    }
}