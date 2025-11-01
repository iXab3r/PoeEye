using System.Reflection;
using System.Runtime.Serialization;
using CommandLine;
using PoeShared.Modularity;
using Parser = CommandLine.Parser;

namespace PoeShared;

public class AppOptions : DisposableReactiveObject
{
    protected const string AutostartFlagValue = "autostart";
    public string AutostartFlag => $"--{AutostartFlagValue}";
    
    [Option( "appName", Default = null, HelpText = "Application name override - displayed in title, used for naming log files, etc")]
    public string AppName { get; set; }

    [Option(AutostartFlagValue, Default = false)]
    public bool IsAutostart { get; set; }
    
    [Option("dataFolder", Default = null)]
    public string DataFolder { get; set; }
    
    [Option('d', "debugMode", Default = false)]
    public bool IsDebugMode { get; set; }
    
    [Option('p', "profile", HelpText = "Profile name - allows to run multiple instances of an application")]
    public string Profile { get; set; }

    [Option('l', "lazyMode", Default = false, HelpText = "Lazy mode - Prism modules will be loaded on-demand")]
    public bool IsLazyMode { get; set; }
        
    [Option('u', "update", Default = false, HelpText = "Show updater before app start")]
    public bool ShowUpdater { get; set; }
    
    [Option('s', "safeMode", Default = null, HelpText = "Safe-Mode - some functions are disabled at startup")]
    public bool? IsSafeMode { get; set; }
    
    [Option('a', "adminMode", Default = null, HelpText = "Admin-Mode - require elevated(admin) mode to function")]
    public bool? IsAdminMode { get; set; }
    
    [Option('m', "modules", HelpText = "Prism modules - Space-separated list of modules that will be loaded")]
    public IEnumerable<string> PrismModules { get; set; }
}

public class AppArguments : AppOptions, IAppArguments
{
    private static readonly string DefaultProfileName = "release";
    private static readonly IFluentLog Log = typeof(AppArguments).PrepareLogger();

    public string AppTitle => $"{(Profile != DefaultProfileName ? $"[{Profile.ToUpper()}]" : string.Empty)}{AppName} v{Version}".ToUpper();

    public Version Version { get; }

    public string AppSupportMail { get; set; } = "";

    public string AppDomainDirectory { get; }

    public string AppDataDirectory { get; }
    
    public string TempDirectory { get; }
    
    public string RoamingAppDataDirectory { get; }

    public string LocalAppDataDirectory { get; }

    public AppArguments()
    {
#if NET5_0_OR_GREATER
        IsWindows = OperatingSystem.IsWindows();
        IsLinux = OperatingSystem.IsLinux();
#else
        IsWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
        IsLinux = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);
#endif
        
        var arguments = CommandLineSplitter.Instance.Split(Environment.CommandLine).ToArray();
        var cmdLineArgs = arguments.Skip(1).ToArray();
        if (cmdLineArgs.Length == 1)
        {
            if (cmdLineArgs.Length == 1 && StringUtils.IsHexGzip(cmdLineArgs[0]))
            {
                Log.Info($"Decompressing arguments: {cmdLineArgs[0]}");
                //compressed arg list
                var decompressed = StringUtils.FromHexGzip(cmdLineArgs[0]);
                Log.Info($"Decompressed arguments: {decompressed}");
                cmdLineArgs = CommandLineSplitter.Instance.Split(decompressed).ToArray();
            }
        }
        var parsed = Parse(cmdLineArgs);
        if (!parsed)
        {
            SharedLog.Instance.InitializeLogging(this);
            throw new ApplicationException($"Failed to parse command line args: {string.Join(" ", CommandLineArguments)}");
        }
        
        var entryAssembly = Assembly.GetEntryAssembly() ?? throw new InvalidOperationException("Entry assembly is not specified");
        Version = entryAssembly.GetName().Version;
       
        if (string.IsNullOrEmpty(AppName))
        {
            //app name is mandatory - supplied either via command line or via assembly attribute
            //do not want to use the process name as it not stable enough
            AppName = (entryAssembly.GetCustomAttribute<AssemblyProductAttribute>() ?? throw new InvalidOperationException($"{nameof(AssemblyProductAttribute)} is not specified on assembly {entryAssembly}")).Product;
        }
        
        ProcessId = Process.GetCurrentProcess().Id;
        IsElevated = true;
        AppDomainDirectory = AppDomain.CurrentDomain.BaseDirectory;
        
        ApplicationExecutablePath = arguments.First();
        ApplicationExecutableName = Path.GetFileName(ApplicationExecutablePath);
        
        var defaultDataFolder = Path.Combine(AppDomainDirectory, "data");
        if (Directory.Exists(defaultDataFolder) && string.IsNullOrEmpty(DataFolder))
        {
            //portable mode with local data folder
            DataFolder = defaultDataFolder;
        }
        
        if (DataFolder != null)
        {
            LocalAppDataDirectory = AppDomain.CurrentDomain.BaseDirectory;
            RoamingAppDataDirectory = DataFolder;
        }
        else
        {
            LocalAppDataDirectory = Path.Combine(EnvironmentLocalAppData.FullName, AppName);
            RoamingAppDataDirectory = Path.Combine(EnvironmentAppData.FullName, AppName);
        }
        
        if (string.IsNullOrEmpty(Profile))
        {
            Profile = IsDebugMode ? "debug" : DefaultProfileName;
        }
        else
        {
            IsDebugMode = string.Equals(Profile, "debug", StringComparison.OrdinalIgnoreCase);
        }
        
        AppDataDirectory = Path.Combine(RoamingAppDataDirectory, Profile);
        TempDirectory = Path.Combine(AppDataDirectory, "temp");

        CommandLineArguments = cmdLineArgs;
        StartupArgs = cmdLineArgs
            .Where(x => !string.Equals(AutostartFlagValue, x, StringComparison.OrdinalIgnoreCase))
            .Where(x => !string.Equals(AutostartFlag, x, StringComparison.OrdinalIgnoreCase))
            .JoinStrings(" ");
        
        var profileSpecificArgsFilePath = Path.Combine(AppDataDirectory, IAppArguments.StartupConfigFileName);
        if (File.Exists(profileSpecificArgsFilePath))
        {
            string argsEx;
            try
            {
                argsEx = File.ReadAllText(profileSpecificArgsFilePath);
                
            }
            catch (Exception e)
            {
                throw new ApplicationException($"Failed to read startup args file @ {profileSpecificArgsFilePath}", e);
            }

            AppArgumentsStartupConfig config;
            try
            {
                config = System.Text.Json.JsonSerializer.Deserialize<AppArgumentsStartupConfig>(argsEx);
                if (config == null)
                {
                    throw new SerializationException($"Failed to deserialized startup args file @ {profileSpecificArgsFilePath}");
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException($"Failed to parse startup args file @ {profileSpecificArgsFilePath}", e);
            }
            
            var cmdLineFromLine = string.IsNullOrEmpty(config.CommandLine) ? string.Empty : config.CommandLine;
            var cmdLineArgsFromFile = CommandLineSplitter.Instance.Split(cmdLineFromLine).ToArray();

            CommandLineArgumentsEx = cmdLineArgsFromFile;
            if (CommandLineArgumentsEx.Length > 0)
            {
                var appOptionsFromFile = new AppOptions();
                if (Parse(appOptionsFromFile, CommandLineArgumentsEx))
                {
                    //at this point we allow only very few specific overrides
                    if (DataFolder != null && !string.IsNullOrEmpty(appOptionsFromFile.AppName))
                    {
                        AppName = appOptionsFromFile.AppName;
                    }
                }
            }
        }
        
        CompositeCommandLineArguments = CommandLineArguments.Concat(CommandLineArgumentsEx).ToArray();
    }

    public bool IsWindows { get; }

    public bool IsLinux { get; }

    public string StartupArgs { get; }
    
    public string[] CommandLineArguments { get; }
    
    public string[] CommandLineArgumentsEx { get; }
    
    public string[] CompositeCommandLineArguments { get; }

    public int ProcessId { get; }

    public bool IsElevated { get; set; }

    public string ApplicationExecutablePath { get; }

    public DirectoryInfo EnvironmentLocalAppData => new(IsWindows ? Environment.ExpandEnvironmentVariables($@"%LOCALAPPDATA%") : Environment.ExpandEnvironmentVariables($"/var/local"));
    
    public DirectoryInfo EnvironmentAppData => new(IsWindows ? Environment.ExpandEnvironmentVariables($@"%APPDATA%") : "/var/roaming");

    public string ApplicationExecutableName { get; }
    
    public bool Parse(string[] args)
    {
        return Parse(this, args);
    }

    public string[] ParseCommandLineArguments(string commandLine)
    {
        return CommandLineSplitter.Instance.Split(Environment.CommandLine).ToArray();
    }

    private static bool Parse(AppOptions instance, string[] args)
    {
        Log.Info($"Parsing command line args: {args.DumpToString()}");
        var parser = new Parser(
            settings =>
            {
                settings.CaseSensitive = false;
                settings.IgnoreUnknownArguments = true;
                settings.ParsingCulture = CultureInfo.InvariantCulture;
                settings.CaseInsensitiveEnumValues = true;
            });
        Log.Info($"Command line parser settings: {parser.Settings.DumpToString()}");
        var result = parser.ParseArguments<AppOptions>(args ?? Array.Empty<string>());
        Log.Info($"Command line parsing result: {result.Tag}, type: {result}");
        switch (result)
        {
            case Parsed<AppOptions> parsedResult:
                parsedResult.Value.CopyPropertiesTo(instance);
                return true;
            case NotParsed<AppOptions> notParsed:
                Log.Warn($"Parsing failed:\n\t{notParsed.Errors.DumpToTable()}");
                return false;
            default:
                Log.Warn($"Parsing failed due to unknown error in parser: {result}");
                return false;
        } 
    }

    protected override void FormatToString(ToStringBuilder builder)
    {
        base.FormatToString(builder);
        builder.Append(new
        {
            AppName,
            Profile,
            IsAutostart,
            AutostartFlag,
            AppSupportMail,
            AppDomainDirectory,
            RoamingAppDataDirectory,
            LocalAppDataDirectory,
            AppDataDirectory,
            TempDirectory,
            ShowUpdater,
            StartupArgs,
            IsSafeMode,
            IsAdminMode,
            ApplicationExecutablePath,
            ApplicationExecutableName
        }.DumpToString());
    }
}