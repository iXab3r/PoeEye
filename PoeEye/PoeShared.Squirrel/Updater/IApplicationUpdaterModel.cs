using System;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Squirrel.Core;
using Squirrel;

namespace PoeShared.Squirrel.Updater
{
    public interface IApplicationUpdaterModel : IDisposableReactiveObject
    {
        UpdateSourceInfo UpdateSource { get; set; }

        bool IgnoreDeltaUpdates { get; set; }

        [CanBeNull] Version UpdatedVersion { get; }

        [CanBeNull] IPoeUpdateInfo LatestVersion { get; }
        
        int ProgressPercent { get; }
        
        bool IsBusy { get; }
        
        /// <summary>
        ///     Checks whether update exist and if so, downloads it
        /// </summary>
        /// <returns>True if application was updated</returns>
        [NotNull]
        Task<IPoeUpdateInfo> CheckForUpdates();

        [NotNull]
        Task RestartApplication();

        Task ApplyRelease([NotNull] IPoeUpdateInfo updateInfo);

        void Reset();

        void HandleSquirrelEvents();

        FileInfo GetLatestExecutable();
    }
}