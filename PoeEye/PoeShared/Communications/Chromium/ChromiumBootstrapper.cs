using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CefSharp;
using CefSharp.Internals;
using CefSharp.OffScreen;
using Guards;
using JetBrains.Annotations;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;

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
            settings.DisableGpuAcceleration();

            settings.MultiThreadedMessageLoop = true;
            settings.ExternalMessagePump = false;
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
            if (!Cef.Initialize(settings, true, new BrowserProcessHandler()))
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

            Log.Instance.Debug("[ChromiumBootstrapper] Awaiting for initialization to be completed");
            isInitialized.WaitOne();

            browserInstance.RequestHandler = new LogRequestHandler();
            browserInstance.RenderProcessMessageHandler = new RenderProcessMessageHandler();

            Log.Instance.Debug("[ChromiumBootstrapper] New browser instance initialized");

            return browserFactory.Create(browserInstance).AddTo(Anchors);
        }

        private static void DisplayBitmap(Task<Bitmap> task)
        {
            // Make a file to save it to (e.g. C:\Users\jan\Desktop\CefSharp screenshot.png)
            var screenshotPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "CefSharp screenshot" + DateTime.Now.Ticks + ".png");

            Console.WriteLine();
            Console.WriteLine("Screenshot ready. Saving to {0}", screenshotPath);

            var bitmap = task.Result;

            // Save the Bitmap to the path.
            // The image type is auto-detected via the ".png" extension.
            bitmap.Save(screenshotPath);

            // We no longer need the Bitmap.
            // Dispose it to avoid keeping the memory alive.  Especially important in 32-bit applications.
            bitmap.Dispose();

            Console.WriteLine("Screenshot saved.  Launching your default image viewer...");

            // Tell Windows to launch the saved image.
            Process.Start(screenshotPath);

            Console.WriteLine("Image viewer launched.  Press any key to exit.");
        }
    }
}
    