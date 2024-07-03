using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using CommandLine;
using PoeShared.Launcher.Services;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using Polly;

namespace PoeShared.Launcher;

public static class Program
{
    private static readonly IFluentLog Log = typeof(Program).PrepareLogger();
    
    public static void Main(string[] args)
    {
        Console.WriteLine($"EyeAuras Launcher");
        HandleStartup();
    }
    
    public static void HandleStartup()
    {
        var result = ParseArguments();
        if (result is not Parsed<LauncherArguments>)
        {
            return;
        }
        result.WithParsed(HandleArguments);
    }

    private static ParserResult<LauncherArguments> ParseArguments()
    {
        var args = Environment.GetCommandLineArgs();
        var result = new Parser(config => config.IgnoreUnknownArguments = true).ParseArguments<LauncherArguments>(args);
        return result;
    }

    private static void HandleArguments(LauncherArguments args)
    {
        try
        {
            var handler = new LauncherServiceHandler();
            handler.AddHandler(nameof(LauncherMethod.Version), HandleVersion);
            handler.AddHandler<RestartAppArguments>(nameof(LauncherMethod.RestartApp), HandleRestart);
            handler.AddHandler<SwapAppArguments>(nameof(LauncherMethod.SwapApp), HandleSwapApp);
            if (handler.TryHandle(args))
            {
                return;
            }

            Log.Info("Terminating the app - work has been done");
            Environment.Exit(0);
        }
        catch (Exception e)
        {
            Log.Error("Operation failed", e);
            throw;
        }
        finally
        {
            if (!args.IsSilent)
            {
                Log.Debug("Press any key to close this window...");
                Console.ReadKey();
            }
        }
    }

    private static void HandleVersion(LauncherArguments args)
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var assemblyName = assembly.GetName();
        Log.Info($"Name: {assemblyName.Name}");
        Log.Info($"Version: {assemblyName.Version}");
    }

    private static void HandleRestart(RestartAppArguments args)
    {
        if (args.ProcessIdToWait != null)
        {
            var timeout = TimeSpan.FromMilliseconds(args.TimeoutMs);
            WaitForProcessExit(args.ProcessIdToWait.Value, timeout);
        }

        StartProcess(args.ExecutablePath, args.Arguments);
    }
    
    private static void HandleSwapApp(SwapAppArguments args)
    {
        if (string.IsNullOrEmpty(args.ExecutablePath))
        {
            throw new ArgumentException(nameof(args), $"Executable path must be specified");
        }
        var timeout = TimeSpan.FromMilliseconds(args.TimeoutMs);
        WaitForProcessExit(args.ProcessIdToWait, timeout);
        
        Policy.Handle<Exception>(ex =>
        {
            Log.Warn($"Exception occured when attempted to remove executable @ {args.ExecutablePath}", ex);
            return true;
        }).WaitAndRetry(new[]
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(3),
        }).Execute(() =>
        {
            var exists = File.Exists(args.ExecutablePath);
            Log.Info($"Trying to remove executable @ {args.ExecutablePath} (exists: {exists})");
            if (exists)
            {
                File.Delete(args.ExecutablePath);
            }
        }); 

        var ownExecutablePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(ownExecutablePath))
        {
            throw new ArgumentException("Failed to get own executable path");
        }
        
        Log.Info($"Copying out own executable from {ownExecutablePath} to {args.ExecutablePath}");
        File.Copy(ownExecutablePath, args.ExecutablePath);

        StartProcess(args.ExecutablePath, args.Arguments);
    }

    private static void StartProcess(string executablePath, string arguments)
    {
        if (string.IsNullOrEmpty(executablePath))
        {
            throw new ArgumentException(nameof(executablePath), $"Executable path must be specified");
        }
        Log.Info($"Spawning new process @ '{executablePath}' (exists: {File.Exists(executablePath)}), args: '{arguments}'");
        var newProcess = Process.Start(executablePath, arguments);
        if (newProcess == null)
        {
            throw new ApplicationException($"Failed to spawn new process @ '{executablePath}' with args '{arguments}'");
        }
        Log.Info($"Spawned new process: {newProcess.Id}");
    }

    private static void WaitForProcessExit(int processId, TimeSpan timeout)
    {
        if (timeout < TimeSpan.Zero)
        {
            throw new ArgumentException("Timeout must be set, but was not");
        }

        try
        {
            Log.Info($"Looking up process {processId}");

            var process = Process.GetProcessById(processId);

            Log.Info($"Found process with Id {processId}, awaiting for termination for {timeout.TotalMilliseconds}ms");
            if (!process.WaitForExit(timeout))
            {
                throw new TimeoutException($"Process {processId} has not exited in time");
            }
            Log.Info("Process has exited, proceeding");
        }
        catch (TimeoutException)
        {
            throw;
        }
        catch (ArgumentException e)
        {
            Log.Warn($"Could not get process with Id {processId}, terminated?", e);
        }
        catch (Exception e)
        {
            Log.Warn($"Something went wrong - failed to await process Id {processId}", e);
            throw;
        }
    }
}