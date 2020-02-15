using System;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PoeShared.Scaffolding;
using Squirrel;

namespace PoeShared.Squirrel.Updater
{
    internal interface IApplicationUpdaterModel : IDisposableReactiveObject
    {
        UpdateSourceInfo UpdateSource { get; set; }

        [CanBeNull] Version UpdatedVersion { get; }

        [CanBeNull] UpdateInfo LatestVersion { get; }
        
        int ProgressPercent { get; }
        
        bool IsBusy { get; }

        /// <summary>
        ///     Checks whether update exist and if so, downloads it
        /// </summary>
        /// <returns>True if application was updated</returns>
        [NotNull]
        Task<UpdateInfo> CheckForUpdates();

        [NotNull]
        Task RestartApplication();

        Task ApplyRelease([NotNull] UpdateInfo updateInfo);

        void Reset();

        FileInfo GetLatestExecutable();
    }
}