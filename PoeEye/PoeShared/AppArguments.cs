using System;
using System.Globalization;
using CommandLine;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeEye
{
    public class AppArguments : DisposableReactiveObject
    {
        private static readonly Lazy<AppArguments> InstanceProducer = new Lazy<AppArguments>();

        public static AppArguments Instance => InstanceProducer.Value;

        public static readonly string AppDataDirectory = Environment.ExpandEnvironmentVariables(@"%APPDATA%\PoeEye");

        private bool isDebugMode;

        [Option('d', "debugMode", DefaultValue = false)] 
        public bool IsDebugMode
        {
            get { return isDebugMode; }
            set { this.RaiseAndSetIfChanged(ref isDebugMode, value); }
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
            return parser.ParseArguments(args ?? new string[0], AppArguments.Instance);
        }
    }
}