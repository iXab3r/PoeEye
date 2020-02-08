using System;
using JetBrains.Annotations;
using PoeShared.Scaffolding;
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

        (string exePath, string exeArgs) GetRestartApplicationArgs();
    }
}