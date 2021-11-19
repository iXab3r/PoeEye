﻿using System;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using PoeShared.Services;
using Squirrel;

namespace PoeShared.Squirrel.Updater
{
    internal sealed class SquirrelEventsHandler : ISquirrelEventsHandler
    {
        private readonly IApplicationAccessor applicationAccessor;
        private static readonly IFluentLog Log = typeof(SquirrelEventsHandler).PrepareLogger();

        public SquirrelEventsHandler(IApplicationAccessor applicationAccessor)
        {
            this.applicationAccessor = applicationAccessor;
            HandleSquirrelEvents();
        }

        private void OnAppUninstall(Version appVersion)
        {
            Log.Debug($"Uninstalling v{appVersion}...");
            applicationAccessor.Terminate(0);
        }

        private void OnAppUpdate(Version appVersion)
        {
            Log.Debug($"Updating v{appVersion}...");
            applicationAccessor.Terminate(0);
        }

        private void OnInitialInstall(Version appVersion)
        {
            Log.Debug($"App v{appVersion} installed");
            applicationAccessor.Terminate(0);
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