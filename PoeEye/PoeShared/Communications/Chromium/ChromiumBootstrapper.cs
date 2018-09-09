using System;
using System.IO;
using System.Reactive.Disposables;
using System.Reflection;
using Guards;
using JetBrains.Annotations;
using PoeShared.Prism;
using PoeShared.Scaffolding;

namespace PoeShared.Communications.Chromium
{
    internal sealed class ChromiumBootstrapper : DisposableReactiveObject, IChromiumBootstrapper
    {
        private readonly string architectureSpecificDirectoryPath;

        public ChromiumBootstrapper(
            [NotNull] IFactory<IChromiumBrowserFactory> bootstrapperFactory)
        {
            Guard.ArgumentNotNull(bootstrapperFactory, nameof(bootstrapperFactory));
            architectureSpecificDirectoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Environment.Is64BitProcess ? "x64" : "x86");

            Log.Instance.Debug(
                $"[ChromiumBootstrapper] Initializing assembly loader(Environment.Is64Bit: {Environment.Is64BitProcess}, OS.Is64Bit: {Environment.Is64BitOperatingSystem}), path: {architectureSpecificDirectoryPath}");
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
            Disposable.Create(() =>
            {
                Log.Instance.Debug("[ChromiumBootstrapper] Uninitialized assembly loader...");
                AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomainOnAssemblyResolve;
            }).AddTo(Anchors);
        }

        private Assembly CurrentDomainOnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (!args.Name.StartsWith("CefSharp"))
            {
                return null;
            }

            var assemblyName = args.Name.Split(new[] {','}, 2)[0] + ".dll";
            var libraryPath = Path.Combine(architectureSpecificDirectoryPath, assemblyName);

            if (!File.Exists(libraryPath))
            {
                Log.Instance.Warn(
                    $"Failed to load '{libraryPath}' (Environment.Is64Bit: {Environment.Is64BitProcess}, OS.Is64Bit: {Environment.Is64BitOperatingSystem})");
                return null;
            }

            return Assembly.LoadFile(libraryPath);
        }
    }
}