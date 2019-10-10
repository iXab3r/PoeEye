using System;
using System.Diagnostics;
using System.Windows.Forms;
using Guards;
using JetBrains.Annotations;
using log4net;
using PoeShared.Scaffolding;
using ReactiveUI;
using StartupHelper;

namespace PoeShared.Services
{
    internal sealed class StartupManager : DisposableReactiveObject, IStartupManager
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(StartupManager));
        private readonly StartupManagerArgs args;

        private readonly StartupHelper.StartupManager manager;
        
        public StartupManager(
            [NotNull] StartupManagerArgs args)
        {
            Guard.ArgumentNotNull(args, nameof(args));
            Log.Debug($"Creating startup helper using args: {args.DumpToTextRaw()}...");

            Guard.ArgumentNotNull(args.ExecutablePath, nameof(args.ExecutablePath));
            Guard.ArgumentNotNull(args.UniqueAppName, nameof(args.UniqueAppName));
            Guard.ArgumentNotNull(args.CommandLineArgs, nameof(args.CommandLineArgs));

            this.args = args;
            manager = new StartupHelper.StartupManager(
                args.ExecutablePath,
                args.UniqueAppName, 
                RegistrationScope.Local,
                false,
                args.AutostartFlag ?? "--autostart");    
            
            Log.Debug($"Manager parameters: {new { ArgsCommandLine = args.CommandLineArgs, manager.IsRegistered, manager.Name, manager.ApplicationImage, manager.RegistrationScope, manager.IsStartedUp, manager.NeedsAdministrativePrivileges, manager.Provider, manager.WorkingDirectory, CommandLineArgs = String.Join(" ", manager.CommandLineArguments), manager.StartupSpecialArgument }}");
            Reregister();
        }

        public bool IsRegistered => manager.IsRegistered;

        public bool Register()
        {
            Log.Debug($"Registering application as {manager.Name} (dir {manager.WorkingDirectory})");
            var result = manager.Register(args.CommandLineArgs);
            if (!result)
            {
                Log.Warn("Failed to register application startup");
            }

            if (result && !IsRegistered)
            {
                Log.Warn("Register returned success, but IsRegistered is still false");
            }
            this.RaisePropertyChanged(nameof(IsRegistered));
            return result;
        }
        
        public bool Unregister()
        {
            Log.Debug($"Unregistering application as {manager.Name} (dir {manager.WorkingDirectory})");

            var result = manager.Unregister();
            if (!result)
            {
                Log.Warn("Failed to unregister application startup");
            }

            if (result && IsRegistered)
            {
                Log.Warn("Unregister returned success, but IsRegistered is still true");
            }
            this.RaisePropertyChanged(nameof(IsRegistered));
            return result;
        }

        private void Reregister()
        {
            if (manager.IsRegistered)
            {
                Log.Debug("Reregistering application startup");
                Unregister();
                Register();
            }
            else
            {
                Log.Debug("Application startup is not registered");
            }
            
        }
    }
}