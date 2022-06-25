using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using CommandLine;
using PoeShared.Modularity;

namespace PoeShared;

public class AppOptions : DisposableReactiveObject
{
    protected const string AutostartFlagValue = "autostart";
    public string AutostartFlag => $"--{AutostartFlagValue}";

    [Option(AutostartFlagValue, Default = false)]
    public bool IsAutostart { get; set; }

    [Option('d', "debugMode", Default = false)]
    public bool IsDebugMode { get; set; }
    
    [Option('p', "profile", HelpText = "Profile name - allows to run multiple instances of an application")]
    public string Profile { get; set; }

    [Option('l', "lazyMode", Default = false, HelpText = "Lazy mode - Prism modules will be loaded on-demand")]
    public bool IsLazyMode { get; set; }
        
    [Option('u', "update", Default = false, HelpText = "Show updater before app start")]
    public bool ShowUpdater { get; set; }
        
    [Option('m', "modules", HelpText = "Prism modules - Space-separated list of modules that will be loaded")]
    public IEnumerable<string> PrismModules { get; set; }
}

public class AppArguments : AppOptions, IAppArguments
{
    private static readonly string DefaultProfileName = "release";
    private static readonly IFluentLog Log = typeof(AppArguments).PrepareLogger();
    private string appName;

    public string AppName
    {
        get => appName ?? throw new ApplicationException($"{nameof(AppName)} must be set beforehand");
        set => appName = value ?? throw new ApplicationException($"{nameof(AppName)} must be set");
    }

    public string AppTitle => $"{(Profile != DefaultProfileName ? $"[{Profile.ToUpper()}]" : string.Empty)}{AppName} v{Version}".ToUpper();

    public Version Version { get; }

    public string AppSupportMail { get; set; } = "";

    public string AppDomainDirectory { get; }

    public string AppDataDirectory { get; }

    public string SharedAppDataDirectory => IsWindows
        ? Environment.ExpandEnvironmentVariables($@"%APPDATA%\{AppName}")
        : $"~/{AppName}";

    public string LocalAppDataDirectory => IsWindows 
        ? Environment.ExpandEnvironmentVariables($@"%LOCALAPPDATA%\{AppName}") 
        : $"~/.{AppName}";

    public AppArguments()
    {
        var entryAssembly = Assembly.GetEntryAssembly() ?? throw new InvalidOperationException("Entry assembly is not specified");
        AppName = (entryAssembly.GetCustomAttribute<AssemblyProductAttribute>() ?? throw new InvalidOperationException($"{nameof(AssemblyProductAttribute)} is not specified on assembly {entryAssembly}")).Product;
        Version = entryAssembly.GetName().Version;
        ProcessId = Process.GetCurrentProcess().Id;
        IsElevated = true;
        AppDomainDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var args = Environment.GetCommandLineArgs();
        StartupArgs = args.Skip(1)
            .Where(x => !string.Equals(AutostartFlagValue, x, StringComparison.OrdinalIgnoreCase))
            .Where(x => !string.Equals(AutostartFlag, x, StringComparison.OrdinalIgnoreCase))
            .JoinStrings(" ");
        ApplicationExecutablePath = args.First();
        ApplicationExecutableName = Path.GetFileName(ApplicationExecutablePath);
        
#if NET5_0_OR_GREATER
            IsWindows = OperatingSystem.IsWindows();
            IsLinux = OperatingSystem.IsLinux();
#else
        IsWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
        IsLinux = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);
#endif
            
        var arguments = Environment.GetCommandLineArgs();
        if (!Parse(arguments))
        {
            SharedLog.Instance.InitializeLogging("Startup", this.AppName);
            throw new ApplicationException($"Failed to parse command line args: {string.Join(" ", arguments)}");
        }

        if (string.IsNullOrEmpty(Profile))
        {
            Profile = IsDebugMode ? "debug" : DefaultProfileName;
        }
        else
        {
            IsDebugMode = Profile == "debug";
        }
        AppDataDirectory = Path.Combine(SharedAppDataDirectory, Profile);

        Log.Debug(() => $"Arguments: {arguments.DumpToString()}");
        Log.Debug(() => $"Parsed args: {this.Dump()}");
    }

    public bool IsWindows { get; }

    public bool IsLinux { get; }

    public string StartupArgs { get; }

    public int ProcessId { get; }

    public bool IsElevated { get; set; }

    public string ApplicationExecutablePath { get; }

    public string ApplicationExecutableName { get; }
        
    public bool Parse(string[] args)
    {
        return Parse(this, args);
    }

    private static bool Parse(AppOptions instance, string[] args)
    {
        Log.Debug(() => $"Parsing command line args: {args.DumpToString()}");
        var parser = new Parser(
            settings =>
            {
                settings.CaseSensitive = false;
                settings.IgnoreUnknownArguments = true;
                settings.ParsingCulture = CultureInfo.InvariantCulture;
            });
        Log.Debug(() => $"Command line parser settings: {parser.Settings.Dump()}");
        var result = parser.ParseArguments<AppOptions>(args ?? Array.Empty<string>());
        Log.Debug(() => $"Command line parsing result: {result.Tag}, type: {result}");
        switch (result)
        {
            case Parsed<AppOptions> parsedResult:
                parsedResult.Value.CopyPropertiesTo(instance);
                return true;
            case NotParsed<AppOptions> notParsed:
                Log.Warn(() => $"Parsing failed:\n\t{notParsed.Errors.DumpToTable()}");
                return false;
            default:
                Log.Warn(() => $"Parsing failed due to unknown error in parser: {result}");
                return false;
        } 
    }

    public override string ToString()
    {
        return new
        {
            AppName,
            Profile,
            IsAutostart,
            AutostartFlag,
            AppSupportMail,
            AppDomainDirectory,
            AppDataDirectory,
            LocalAppDataDirectory,
            ShowUpdater,
            StartupArgs,
            ApplicationPath = ApplicationExecutablePath,
            ApplicationName = ApplicationExecutableName
        }.Dump();
    }
}