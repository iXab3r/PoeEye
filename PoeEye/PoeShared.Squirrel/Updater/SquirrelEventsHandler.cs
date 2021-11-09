using System;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using Squirrel;

namespace PoeShared.Squirrel.Updater
{
    internal sealed class SquirrelEventsHandler : ISquirrelEventsHandler
    {
        private static readonly IFluentLog Log = typeof(SquirrelEventsHandler).PrepareLogger();

        public SquirrelEventsHandler()
        {
            HandleSquirrelEvents();
        }

        private void OnAppUninstall(Version appVersion)
        {
            Log.Debug($"Uninstalling v{appVersion}...");
            throw new NotSupportedException("Should never be invoked");
        }

        private void OnAppUpdate(Version appVersion)
        {
            Log.Debug($"Updating v{appVersion}...");
            throw new NotSupportedException("Should never be invoked");
        }

        private void OnInitialInstall(Version appVersion)
        {
            Log.Debug($"App v{appVersion} installed");
            throw new NotSupportedException("Should never be invoked");
        }

        private void OnFirstRun()
        {
            Log.Debug("App started for the first time");
        }

        private void HandleSquirrelEvents()
        {
            Log.Debug("Handling Squirrel events");
            SquirrelAwareApp.HandleEvents(
                OnInitialInstall,
                OnAppUpdate,
                onAppUninstall: OnAppUninstall,
                onFirstRun: OnFirstRun);
            Log.Debug("Squirrel events were handled successfully");
        }
    }
}