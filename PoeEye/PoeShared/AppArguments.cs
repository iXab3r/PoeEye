using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using CommandLine;
using log4net;
using PoeShared.Modularity;
using PoeShared.Scaffolding;

namespace PoeShared
{
    public class AppOptions : DisposableReactiveObject
    {
        protected const string AutostartFlagValue = "autostart";

        private bool isDebugMode;
        private bool isLazyMode;
        private bool isAutostart;
        public string AutostartFlag => $"--{AutostartFlagValue}";

        [Option(AutostartFlagValue, Default = false)]
        public bool IsAutostart
        {
            get => isAutostart;
            set => this.RaiseAndSetIfChanged(ref isAutostart, value);
        }
        
        [Option('d', "debugMode", Default = false)]
        public bool IsDebugMode
        {
            get => isDebugMode;
            set => this.RaiseAndSetIfChanged(ref isDebugMode, value);
        }
        
        [Option('l', "lazyMode", Default = false, HelpText = "Lazy mode - Prism modules will be loaded on-demand")]
        public bool IsLazyMode
        {
            get => isLazyMode;
            set => this.RaiseAndSetIfChanged(ref isLazyMode, value);
        }
    }

    public class AppArguments : AppOptions, IAppArguments
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AppArguments));

        private static readonly Lazy<AppArguments> InstanceProducer = new Lazy<AppArguments>(() => new AppArguments());
        public static AppArguments Instance => InstanceProducer.Value;

        private bool isElevated;
        private string appName;

        public string AppName
        {
            get => appName ?? throw new ApplicationException($"{nameof(AppName)} must be set beforehand");
            set => appName = value ?? throw new ApplicationException($"{nameof(AppName)} must be set");
        }

        public string AppSupportMail { get; set; } = "";
        
        public string AppDomainDirectory => AppDomain.CurrentDomain.BaseDirectory;
        
        public string AppDataDirectory => IsWindows ? Environment.ExpandEnvironmentVariables($@"%APPDATA%\{AppName}") : $"~/{AppName}";
        
        public string LocalAppDataDirectory => IsWindows ? Environment.ExpandEnvironmentVariables($@"%LOCALAPPDATA%\{AppName}") : $"~/.{AppName}";

        public AppArguments()
        {
            ProcessId = Process.GetCurrentProcess().Id;
            IsElevated = true;
            var args = Environment.GetCommandLineArgs();
            StartupArgs = args.Skip(1)
                .Where(x => !string.Equals(AutostartFlagValue, x, StringComparison.OrdinalIgnoreCase))
                .Where(x => !string.Equals(AutostartFlag, x, StringComparison.OrdinalIgnoreCase))
                .JoinStrings(" ");
            ApplicationExecutablePath = args.First();
            ApplicationExecutableName = Path.GetFileName(ApplicationExecutablePath);
            IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        }
        
        public bool IsWindows { get; }
        
        public bool IsLinux { get; }
        
        public string StartupArgs { get; }
        
        public int ProcessId { get; }
        
        public bool IsElevated
        {
            get => isElevated;
            set => this.RaiseAndSetIfChanged(ref isElevated, value);
        }
        
        public string ApplicationExecutablePath { get; }
        
        public string ApplicationExecutableName { get; }

        public static bool Parse(string[] args)
        {
            Log.Debug($"Parsing command line args: {args.DumpToTextRaw()}");
            var parser = new Parser(
                settings =>
                {
                    settings.CaseSensitive = false;
                    settings.IgnoreUnknownArguments = true;
                    settings.ParsingCulture = CultureInfo.InvariantCulture;
                });
            Log.Debug($"Command line parser settings: {parser.Settings.DumpToTextRaw()}");
            var result = parser.ParseArguments<AppOptions>(args ?? new string[0]);
            Log.Debug($"Command line parsing result: {result.Tag}, type: {result}");
            if (result.Tag == ParserResultType.Parsed && result is Parsed<AppOptions> parsedResult)
            {
                parsedResult.Value.CopyPropertiesTo(Instance);
                return true;
            }
            return false;
        }
        
        public override string ToString()
        {
            return new
            {
                AppName,
                IsAutostart,
                AutostartFlag,
                AppSupportMail,
                AppDomainDirectory,
                AppDataDirectory,
                LocalAppDataDirectory,
                IsDebugMode,
                StartupArgs,
                ApplicationPath = ApplicationExecutablePath,
                ApplicationName = ApplicationExecutableName
            }.DumpToTextRaw();
        }
    }
}