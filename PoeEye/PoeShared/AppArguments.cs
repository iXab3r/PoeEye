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

        public static readonly string AppDataDirectory = Environment.ExpandEnvironmentVariables(@"%APPDATA%\PoeEye");
        public static readonly string LocalAppDataDirectory = Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\PoeEye");
        public static readonly string PoeEyeMail = "mail.poeeye@gmail.com";

        private bool isDebugMode;

        public static AppArguments Instance => InstanceProducer.Value;

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
    }
}