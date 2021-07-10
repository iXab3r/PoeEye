using System;
using System.IO;
using System.Windows.Input;
using JetBrains.Annotations;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Scaffolding.WPF;

namespace PoeShared.Squirrel.Updater
{
    public interface IApplicationUpdaterViewModel : IDisposableReactiveObject
    {
        [NotNull] CommandWrapper CheckForUpdatesCommand { get; }

        [NotNull] CommandWrapper RestartCommand { get; }

        [NotNull] CommandWrapper ApplyUpdate { get; }

        bool IsInErrorStatus { get; }

        string StatusText { get; }
        
        int ProgressPercent { get; }

        bool IsOpen { get; set; }

        UpdateSourceInfo UpdateSource { get; }
        
        Version UpdatedVersion { get; }

        Version LatestVersion { get; }

        bool CheckForUpdates { get; set; }

        [NotNull] CommandWrapper OpenUri { get; }

        FileInfo GetLatestExecutable();
    }
}