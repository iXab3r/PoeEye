﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using CommandLine;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Scaffolding;

namespace PoeShared
{
    public class AppArguments : DisposableReactiveObject, IAppArguments
    {
        private static readonly Lazy<AppArguments> InstanceProducer = new Lazy<AppArguments>(() => new AppArguments());
        public static AppArguments Instance => InstanceProducer.Value;

        private bool isDebugMode;
        private bool isLazyMode;
        private bool isAutostart;

        private const string AutostartFlagValue = "autostart";

        public string AppName { get; set; } = "PoeEye";
        public string AppSupportMail { get; set; } = "mail.poeeye@gmail.com";
        
        public string AppDomainDirectory => AppDomain.CurrentDomain.BaseDirectory;
        public string AppDataDirectory => Environment.ExpandEnvironmentVariables($@"%APPDATA%\{AppName}");
        public string LocalAppDataDirectory => Environment.ExpandEnvironmentVariables($@"%LOCALAPPDATA%\{AppName}");

        private AppArguments()
        {
            ProcessId = Process.GetCurrentProcess().Id;
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                IsElevated = UnsafeNative.IsElevated();
            } else
            {
                IsElevated = true;
            }
            var args = Environment.GetCommandLineArgs();
            StartupArgs = args.Skip(1)
                .Where(x => !string.Equals(AutostartFlagValue, x, StringComparison.OrdinalIgnoreCase))
                .Where(x => !string.Equals(AutostartFlag, x, StringComparison.OrdinalIgnoreCase))
                .JoinStrings(" ");
            ApplicationExecutablePath = args.First();
            ApplicationExecutableName = Path.GetFileName(ApplicationExecutablePath);
        }

        public string AutostartFlag => $"--{AutostartFlagValue}";

        public string StartupArgs { get; }
        
        public int ProcessId { get; }
        
        public bool IsElevated { get; }
        
        public string ApplicationExecutablePath { get; }
        
        public string ApplicationExecutableName { get; }

        [Option(AutostartFlagValue, DefaultValue = false)]
        public bool IsAutostart
        {
            get => isAutostart;
            set => this.RaiseAndSetIfChanged(ref isAutostart, value);
        }
        
        [Option('d', "debugMode", DefaultValue = false)]
        public bool IsDebugMode
        {
            get => isDebugMode;
            set => this.RaiseAndSetIfChanged(ref isDebugMode, value);
        }
        
        [Option('l', "lazyMode", DefaultValue = false, HelpText = "Lazy mode - Prism modules will be loaded on-demand")]
        public bool IsLazyMode
        {
            get => isLazyMode;
            set => this.RaiseAndSetIfChanged(ref isLazyMode, value);
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