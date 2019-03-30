using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using CommandLine;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeEye
{
    public class AppArguments : DisposableReactiveObject
    {
        private static readonly Lazy<AppArguments> InstanceProducer = new Lazy<AppArguments>(() => new AppArguments());

        private bool isDebugMode;

        public static AppArguments Instance => InstanceProducer.Value;

        public string AppName { get; set; } = "PoeEye";
        public string AppSupportMail { get; set; } = "mail.poeeye@gmail.com";
        
        public string AppDomainDirectory => AppDomain.CurrentDomain.BaseDirectory;
        public string AppDataDirectory => Environment.ExpandEnvironmentVariables($@"%APPDATA%\{AppName}");
        public string LocalAppDataDirectory => Environment.ExpandEnvironmentVariables($@"%LOCALAPPDATA%\{AppName}");

        private AppArguments()
        {
            StartupArgs = Environment.GetCommandLineArgs().Skip(1).FirstOrDefault();
        }

        public string StartupArgs { get; }

        [Option('d', "debugMode", DefaultValue = false)]
        public bool IsDebugMode
        {
            get => isDebugMode;
            set => this.RaiseAndSetIfChanged(ref isDebugMode, value);
        }

        public static bool Parse(string[] args)
        {
            var parser = new Parser(
                settings =>
                {
                    settings.CaseSensitive = false;
                    settings.IgnoreUnknownArguments = true;
                    settings.ParsingCulture = CultureInfo.InvariantCulture;
                    settings.MutuallyExclusive = false;
                });
            return parser.ParseArguments(args ?? new string[0], Instance);
        }

        public override string ToString()
        {
            return new
            {
                AppName,
                AppSupportMail,
                AppDomainDirectory,
                AppDataDirectory,
                LocalAppDataDirectory,
                IsDebugMode,
                StartupArgs
            }.ToString();
        }
    }
}