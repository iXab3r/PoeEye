using System;
using System.Globalization;
using CommandLine;

namespace PoeEye
{
    public class AppArguments
    {
        private static readonly Lazy<AppArguments> InstanceProducer = new Lazy<AppArguments>();

        public static AppArguments Instance => InstanceProducer.Value;

        [Option('d', "debugMode", DefaultValue = false)]
        public bool IsDebugMode { get; set; }

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