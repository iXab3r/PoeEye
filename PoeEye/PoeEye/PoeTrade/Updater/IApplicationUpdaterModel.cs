using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace PoeEye.PoeTrade.Updater
{
    internal interface IApplicationUpdaterModel : IDisposableReactiveObject
    {
        [NotNull]
        Version MostRecentVersion { get; }

        /// <summary>
        ///     Checks whether update exist and if so, downloads it
        /// </summary>
        /// <returns>True if application was updated</returns>
        [NotNull]
        Task<bool> CheckForUpdates();

        [NotNull]
        Task RestartApplication();
    }
}