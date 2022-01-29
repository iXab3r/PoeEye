using System;
using JetBrains.Annotations;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using StartupHelper;

namespace PoeShared.Services;

internal sealed class StartupManager : DisposableReactiveObject, IStartupManager
{
    private static readonly IFluentLog Log = typeof(StartupManager).PrepareLogger();
    private readonly IAppArguments appArguments;
    private readonly StartupManagerArgs args;

    private readonly StartupHelper.StartupManager manager;
        
    public StartupManager(
        [NotNull] IAppArguments appArguments,
        [NotNull] StartupManagerArgs args)
    {
        Guard.ArgumentNotNull(appArguments, nameof(appArguments));
        Guard.ArgumentNotNull(args, nameof(args));
        Log.Debug(() => $"Creating startup helper using args: {args.DumpToTextRaw()}, isElevated: {appArguments.IsElevated}...");

        Guard.ArgumentNotNull(args.ExecutablePath, nameof(args.ExecutablePath));
        Guard.ArgumentNotNull(args.UniqueAppName, nameof(args.UniqueAppName));
        Guard.ArgumentNotNull(args.CommandLineArgs, nameof(args.CommandLineArgs));

        this.appArguments = appArguments;
        this.args = args;
        try
        {
            manager = new StartupHelper.StartupManager(
                args.ExecutablePath,
                args.UniqueAppName, 
                RegistrationScope.Local,
                true,
                StartupProviders.Task,
                args.AutostartFlag ?? appArguments.AutostartFlag);
            Log.Debug(() => $"Manager parameters: {new { ArgsCommandLine = args.CommandLineArgs, manager.IsRegistered, manager.Name, manager.ApplicationImage, manager.RegistrationScope, manager.IsStartedUp, manager.NeedsAdministrativePrivileges, manager.Provider, manager.WorkingDirectory, CommandLineArgs = String.Join(" ", manager.CommandLineArguments), manager.StartupSpecialArgument }}");
                
            if (IsElevated)
            {
                Log.Warn($"Application has Admin privileges, initializing startup manager");
                Reregister();
            }
            else
            {
                Log.Warn($"Manager is not available due to lack of Admin privileges");
            }
        }
        catch (Exception e)
        {
            Log.Error("Failed to initialize startup manager", e);
        }    
    }

    public bool IsRegistered => IsReady && manager.IsRegistered;
    
    public bool IsReady => manager != null;

    public bool IsElevated => IsReady && appArguments.IsElevated;
    
    public bool Register()
    {
        EnsureManagerIsReady();
            
        Log.Debug(() => $"Registering application as {manager.Name} (dir {manager.WorkingDirectory})");
        var result = manager.Register(args.CommandLineArgs);
        if (!result)
        {
            Log.Warn($"Failed to register application startup");
        }

        if (result && !IsRegistered)
        {
            Log.Warn($"Register returned success, but IsRegistered is still false");
        }
        this.RaisePropertyChanged(nameof(IsRegistered));
        return result;
    }
        
    public bool Unregister()
    {
        EnsureManagerIsReady();
            
        Log.Debug(() => $"Unregistering application as {manager.Name} (dir {manager.WorkingDirectory})");

        var result = manager.Unregister();
        if (!result)
        {
            Log.Warn($"Failed to unregister application startup");
        }

        if (result && IsRegistered)
        {
            Log.Warn($"Unregister returned success, but IsRegistered is still true");
        }
        this.RaisePropertyChanged(nameof(IsRegistered));
        return result;
    }

    private void Reregister()
    {
        EnsureManagerIsReady();
        if (manager.IsRegistered)
        {
            Log.Debug(() => $"Reregistering application startup");
            Unregister();
            Register();
        }
        else
        {
            Log.Debug(() => $"Application startup is not registered");
        }
    }

    private void EnsureElevated()
    {
        if (!IsElevated)
        {
            throw new InvalidOperationException("Operation is not available without Admin permissions");
        }
    }
    
    private void EnsureManagerIsReady()
    {
        EnsureElevated();
        
        if (manager == null)
        {
            throw new InvalidOperationException("Startup manager is not ready");
        }
    }
}