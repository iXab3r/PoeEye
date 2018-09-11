using System;
using System.IO;
using System.Reactive.Disposables;
using System.Reflection;
using System.Runtime.CompilerServices;
using Guards;
using JetBrains.Annotations;
using PoeShared.Prism;
using PoeShared.Scaffolding;

namespace PoeShared.Communications.Chromium
{
    internal sealed class ChromiumBootstrapper : DisposableReactiveObject, IChromiumBootstrapper
    {
        private static readonly string AssemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        private readonly string ArchitectureSpecificDirectoryPath;

        public ChromiumBootstrapper(
            [NotNull] IFactory<IChromiumBrowserFactory> bootstrapperFactory)
        {
            Guard.ArgumentNotNull(bootstrapperFactory, nameof(bootstrapperFactory));
            ArchitectureSpecificDirectoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Environment.Is64BitProcess ? "x64" : "x86");

            Log.Instance.Debug(
                $"[ChromiumBootstrapper] Initializing assembly loader(Environment.Is64Bit: {Environment.Is64BitProcess}, OS.Is64Bit: {Environment.Is64BitOperatingSystem}), path: {ArchitectureSpecificDirectoryPath}");
            
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;

            Disposable.Create(() =>
            {
                Log.Instance.Debug("[ChromiumBootstrapper] Uninitialized assembly loader...");
                AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomainOnAssemblyResolve;
            }).AddTo(Anchors);
            
            InitializeCef().AddTo(Anchors);
        }

        private Assembly CurrentDomainOnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (!args.Name.StartsWith("CefSharp"))
            {
                return null;
            }

            var assemblyName = args.Name.Split(new[] {','}, 2)[0] + ".dll";
            var libraryPath = Path.Combine(ArchitectureSpecificDirectoryPath, assemblyName);

            if (!File.Exists(libraryPath))
            {
                Log.Instance.Warn(
                    $"Failed to load '{libraryPath}' (Environment.Is64Bit: {Environment.Is64BitProcess}, OS.Is64Bit: {Environment.Is64BitOperatingSystem})");
                return null;
            }

            return Assembly.LoadFile(libraryPath);
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static IDisposable InitializeCef()
        {
            Log.Instance.Debug("[ChromiumBrowserFactory] Initializing CEF...");
            var anchors = new CompositeDisposable();


            if (CefSharp.Cef.IsInitialized)
            {
                throw new ApplicationException("CEF is already initialized");
            }

            var settings = new CefSharp.CefSettings();
            settings.DisableGpuAcceleration();

            settings.MultiThreadedMessageLoop = true;
            settings.ExternalMessagePump = false;
            settings.CachePath = "cefcache";
            settings.SetOffScreenRenderingBestPerformanceArgs();
            settings.WindowlessRenderingEnabled = true;
            if (Log.Instance.IsTraceEnabled)
            {
                settings.LogSeverity = CefSharp.LogSeverity.Verbose;
            }
            else if (Log.Instance.IsDebugEnabled)
            {
                settings.LogSeverity = CefSharp.LogSeverity.Error;
            }
            else
            {
                settings.LogSeverity = CefSharp.LogSeverity.Disable;
            }

            settings.IgnoreCertificateErrors = true;
            settings.CefCommandLineArgs.Add("no-proxy-server", "1");
            settings.UserAgent = "CefSharp Browser" + CefSharp.Cef.CefSharpVersion;
            settings.CefCommandLineArgs.Add("disable-extensions", "1");
            settings.CefCommandLineArgs.Add("disable-pdf-extension", "1");

            settings.BrowserSubprocessPath = Path.Combine(AssemblyDir,
                                                          Environment.Is64BitProcess ? "x64" : "x86",
                                                          "CefSharp.BrowserSubprocess.exe");

            Log.Instance.Debug($"[ChromiumBrowserFactory] CEF settings: {settings.DumpToTextRaw()}");
            if (!CefSharp.Cef.Initialize(settings, true, new BrowserProcessHandler()))
            {
                throw new ApplicationException("Failed to initialize CEF");
            }

            Disposable.Create(
                () =>
                {
                    Log.Instance.Debug("[ChromiumBrowserFactory] Shutting down CEF...");
                    CefSharp.Cef.Shutdown();
                    Log.Instance.Debug("[ChromiumBrowserFactory] CEF has been shut down");
                }).AddTo(anchors);

            return anchors;
        }
    }
}