using System.Diagnostics;
using System.Reactive.Subjects;
using System.Reflection;

using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Filter;
using log4net.Layout;
using log4net.Repository;
using log4net.Repository.Hierarchy;
using PoeShared.Modularity;

namespace PoeShared;

public class SharedLog : DisposableReactiveObject
{
    /// <summary>
    ///     Log Instance HAVE to be initialized only after GlobalContext is configured
    /// </summary>
    private static readonly Lazy<IFluentLog> LogInstanceSupplier = new(() =>
    {
        var log = LogManager.GetLogger(typeof(SharedLog));
        log.Debug($"Logger instance initialized, context: {GlobalContext.Properties.Dump()}");
        var process = Process.GetCurrentProcess();
        return log.ToFluent().WithSuffix($"{process.ProcessName} PID {process.Id}");
    });

    private static readonly Lazy<SharedLog> InstanceSupplier = new();

    public SharedLog()
    {
        Errors.Subscribe(ex => { Log.HandleException(ex); }).AddTo(Anchors);
    }

    public ISubject<Exception> Errors { get; } = new Subject<Exception>();

    public static SharedLog Instance => InstanceSupplier.Value;

    public IFluentLog Log => LogInstanceSupplier.Value;

    public void InitializeLogging(IAppArguments appArguments)
    {
        Guard.ArgumentNotNull(appArguments.Profile, nameof(appArguments.Profile));

        GlobalContext.Properties["configuration"] = appArguments.Profile;
        GlobalContext.Properties["CONFIGURATION"] = appArguments.Profile;
        GlobalContext.Properties["APPNAME"] = appArguments.AppName;
        GlobalContext.Properties["APPDATA"] = appArguments.AppDataDirectory;
        GlobalContext.Properties["LOCALAPPDATA"] = appArguments.LocalAppDataDirectory;

        var startupInfo =
            $"Logging for app {GlobalContext.Properties["APPNAME"]} in '{GlobalContext.Properties["CONFIGURATION"]}' mode initialized";
        Console.WriteLine(startupInfo);
        Trace.WriteLine(startupInfo);
    }

    public void LoadLogConfiguration(IAppArguments appArguments, FileInfo logConfig)
    {
        Guard.ArgumentNotNull(logConfig, nameof(logConfig));
        if (!logConfig.Exists)
        {
            throw new FileNotFoundException($"Log config file not found: {logConfig.FullName}", logConfig.FullName);
        }

        var repository = (Hierarchy)  LogManager.GetRepository(Assembly.GetEntryAssembly());
        XmlConfigurator.ConfigureAndWatch(repository, logConfig);
        Log.Info($"Logging settings loaded from {logConfig}");
        DumpApplicationInfo(appArguments);
    }
    
    public void SetLoggerLevel(string loggerName, Level level)
    {
        var hierarchy = (Hierarchy)LogManager.GetRepository(Assembly.GetEntryAssembly());
    
        // Get or create logger
        var logger = hierarchy.GetLogger(loggerName) as Logger;
        if (logger != null)
        {
            logger.Level = level;
            logger.Additivity = true; // true means logs bubble up to root, set false if you don't want that
        }
    }

    public void DumpApplicationInfo(IAppArguments appArguments)
    {
        Log.Info($"Parsed args: {appArguments.Dump()}");
        Log.Info($"CmdLine: {Environment.CommandLine}");
        Log.Info($"CommandLineArgs: {appArguments.StartupArgs}");
        Log.Info($"Time: {  new { DateTime.UtcNow, DateTime.Now, TimeZoneLocal = TimeZoneInfo.Local } }");
        Log.Info($"AppDomain: { new { AppDomain.CurrentDomain.Id, AppDomain.CurrentDomain.FriendlyName, AppDomain.CurrentDomain.BaseDirectory,  AppDomain.CurrentDomain.DynamicDirectory }})");
        Log.Info($"Assemblies: { new { Entry = Assembly.GetEntryAssembly(), Executing = Assembly.GetExecutingAssembly(), Calling = Assembly.GetCallingAssembly() }})");
        Log.Info($"OS: { new { Environment.OSVersion, Environment.Is64BitProcess, Environment.Is64BitOperatingSystem }})");
        Log.Info($"Runtime: {new { System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription, System.Runtime.InteropServices.RuntimeInformation.OSDescription, OSVersion = Environment.OSVersion.Version }}");
        Log.Info($"Culture: {Thread.CurrentThread.CurrentCulture}, UICulture: {Thread.CurrentThread.CurrentUICulture}");
        Log.Info($"Is Elevated: {appArguments.IsElevated}");
        Log.Info($"Environment: {new { Environment.MachineName, Environment.UserName, Environment.WorkingSet, Environment.SystemDirectory, Environment.UserInteractive }})");
        Log.Info($"FileSystem: {new { Path.PathSeparator, Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, Path.VolumeSeparatorChar }})");
    }

    public void SwitchLoggingLevel(Level loggingLevel)
    {
        Guard.ArgumentNotNull(loggingLevel, nameof(loggingLevel));

        var repository = (Hierarchy)  LogManager.GetRepository(Assembly.GetEntryAssembly());
        repository.Root.Level = loggingLevel;
        repository.RaiseConfigurationChanged(EventArgs.Empty);
        Log.Warn($"Logging level switched to '{loggingLevel}'");
    }
    
    public void SwitchImmediateFlush(bool immediateFlush)
    {
        var repository = (Hierarchy)  LogManager.GetRepository(Assembly.GetEntryAssembly());

        var updatedAppenders = new List<FileAppender>();
        foreach (var appender in repository.GetAppenders().OfType<FileAppender>())
        {
            if (appender.ImmediateFlush == immediateFlush)
            {
                continue;
            }
            appender.ImmediateFlush = immediateFlush;
            updatedAppenders.Add(appender);
        }

        if (!updatedAppenders.Any())
        {
            return;
        }

        repository.RaiseConfigurationChanged(EventArgs.Empty);
        Log.Warn($"ImmediateFlush switched to {immediateFlush} for {updatedAppenders.Count} appender(s):\n\t{updatedAppenders.Select(x => new { x.Name, x.File, x.Threshold, x.LockingModel }).DumpToTable()}");
    }

    public IDisposable AddTraceAppender()
    {
        var listener = new Log4NetTraceListener(Log);
        Log.Debug($"Adding TraceListener");
        Trace.Listeners.Add(listener);
        Trace.WriteLine("TraceListener initialized");
        return Disposable.Create(() =>
        {
            Log.Debug($"Removing TraceListener");
            Trace.Listeners.Remove(listener);
        });
    }

    public IDisposable AddAppender(IAppender appender, Hierarchy repository)
    {
        var root = repository.Root;
        Log.Debug($"Adding appender {appender}, currently root contains {root.Appenders.Count} appenders");
        root.AddAppender(appender);
        if (!repository.Configured)
        {
            Log.Debug($"Repository is not configured, invoking basic configuration");
            BasicConfigurator.Configure(repository);
        }
        repository.RaiseConfigurationChanged(EventArgs.Empty);

        return Disposable.Create(() =>
        {
            Log.Debug($"Removing appender {appender}, currently root contains {root.Appenders.Count} appenders");
            root.RemoveAppender(appender);
            repository.RaiseConfigurationChanged(EventArgs.Empty);
        });
    }
    
    public IDisposable AddAppender(IAppender appender)
    {
        Guard.ArgumentNotNull(appender, nameof(appender));
        var repository = (Hierarchy)  LogManager.GetRepository(Assembly.GetEntryAssembly());
        return AddAppender(appender, repository);
    }

    public IDisposable AddConsoleAppender()
    {
        var consoleAppender = new ConsoleAppender()
        {
            Threshold = Level.All,
            Layout = new PatternLayout()
            {
                ConversionPattern = "%date [%-2thread] %-5level %message [%logger]%newline"
            }
        };
        return AddAppender(consoleAppender);
    }

    public IDisposable AddLocalLogFileAppender()
    {
        return AddLocalLogFileAppender($"logs/app.log", Level.All);
    }
    
    public IDisposable AddLocalLogFileAppender(string filePathPattern, Level logLevel)
    {
        var layout = new PatternLayout
        {
            ConversionPattern = "%date [%-2thread] %-5level %message [%logger]%newline"
        };
        layout.ActivateOptions();

        var fileAppender = new RollingFileAppender
        {
            Name = "LocalFileAppender",
            Threshold = logLevel,
            StaticLogFileName = false,
            File = filePathPattern,
            ImmediateFlush = true,
            AppendToFile = false,
            MaxFileSize = 1024 * 1024 * 50,
            RollingStyle = RollingFileAppender.RollingMode.Composite,
            PreserveLogFileNameExtension = true,
            MaxSizeRollBackups = 5,
            LockingModel = new FileAppender.ExclusiveLock(),
            Layout = layout
        };

        ((PatternLayout)fileAppender.Layout).ActivateOptions();
        fileAppender.ActivateOptions();
        
        return AddAppender(fileAppender);
    }
        
    private sealed class Log4NetTraceListener : System.Diagnostics.TraceListener
    {
        private readonly IFluentLog log;

        public Log4NetTraceListener(IFluentLog log)
        {
            this.log = log;
        }

        public override void Write(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }
            log.Debug($"[TraceListener] {message}");
        }

        public override void WriteLine(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }
            log.Debug($"[TraceListener] {message}");
        }
    }
}