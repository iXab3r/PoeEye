using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using NuGet;
using PoeShared.Squirrel.Scaffolding;
using Splat;
using Squirrel;
using Squirrel.Shell;

namespace PoeShared.Squirrel.Core
{
    public sealed partial class PoeUpdateManager : IUpdateManager
    {
        private static bool Exiting;
        private readonly string updateUrlOrPath;
        private readonly IFileDownloader urlDownloader;

        private IDisposable updateLock;

        public PoeUpdateManager(string urlOrPath,
            string applicationName = null,
            string rootDirectory = null,
            IFileDownloader urlDownloader = null)
        {
            Guard.ArgumentIsTrue(!string.IsNullOrEmpty(urlOrPath), nameof(urlOrPath));
            Guard.ArgumentIsTrue(!string.IsNullOrEmpty(urlOrPath), "!string.IsNullOrEmpty(urlOrPath)");
            Guard.ArgumentIsTrue(!string.IsNullOrEmpty(applicationName), "!string.IsNullOrEmpty(applicationName)");

            updateUrlOrPath = urlOrPath;
            ApplicationName = applicationName ?? GetApplicationName();
            this.urlDownloader = urlDownloader ?? new FileDownloader();

            RootAppDirectory = Path.Combine(rootDirectory ?? GetLocalAppDataDirectory(), ApplicationName);
        }

        public string ApplicationName { get; }

        public string RootAppDirectory { get; }

        public bool IsInstalledApp => Assembly.GetExecutingAssembly().Location.StartsWith(RootAppDirectory, StringComparison.OrdinalIgnoreCase);

        public async Task<UpdateInfo> CheckForUpdate(bool ignoreDeltaUpdates = false, Action<int> progress = null)
        {
            var checkForUpdate = new CheckForUpdateImpl(RootAppDirectory);

            await AcquireUpdateLock();
            return await checkForUpdate.CheckForUpdate(
                Utility.LocalReleaseFileForAppDir(RootAppDirectory),
                updateUrlOrPath,
                ignoreDeltaUpdates,
                progress,
                urlDownloader);
        }

        public async Task DownloadReleases(IEnumerable<ReleaseEntry> releasesToDownload, Action<int> progress = null)
        {
            var downloadReleases = new DownloadReleasesImpl(RootAppDirectory);
            await AcquireUpdateLock();

            await downloadReleases.DownloadReleases(updateUrlOrPath, releasesToDownload, progress, urlDownloader);
        }

        public async Task<string> ApplyReleases(UpdateInfo updateInfo, Action<int> progress = null)
        {
            var applyReleases = new ApplyReleasesImpl(RootAppDirectory);
            await AcquireUpdateLock();

            return await applyReleases.ApplyReleases(updateInfo, false, false, progress);
        }

        public async Task FullInstall(bool silentInstall = false, Action<int> progress = null)
        {
            var updateInfo = await CheckForUpdate();
            await DownloadReleases(updateInfo.ReleasesToApply);

            var applyReleases = new ApplyReleasesImpl(RootAppDirectory);
            await AcquireUpdateLock();

            await applyReleases.ApplyReleases(updateInfo, silentInstall, true, progress);
        }

        public async Task FullUninstall()
        {
            var applyReleases = new ApplyReleasesImpl(RootAppDirectory);
            await AcquireUpdateLock();

            KillAllExecutablesBelongingToPackage();
            await applyReleases.FullUninstall();
        }

        public Task<RegistryKey> CreateUninstallerRegistryEntry(string uninstallCmd, string quietSwitch)
        {
            var installHelpers = new InstallHelperImpl(ApplicationName, RootAppDirectory);
            return installHelpers.CreateUninstallerRegistryEntry(uninstallCmd, quietSwitch);
        }

        public Task<RegistryKey> CreateUninstallerRegistryEntry()
        {
            var installHelpers = new InstallHelperImpl(ApplicationName, RootAppDirectory);
            return installHelpers.CreateUninstallerRegistryEntry();
        }

        public void RemoveUninstallerRegistryEntry()
        {
            var installHelpers = new InstallHelperImpl(ApplicationName, RootAppDirectory);
            installHelpers.RemoveUninstallerRegistryEntry();
        }

        public void CreateShortcutsForExecutable(string exeName, ShortcutLocation locations, bool updateOnly, string programArguments = null,
            string icon = null)
        {
            var installHelpers = new ApplyReleasesImpl(RootAppDirectory);
            installHelpers.CreateShortcutsForExecutable(exeName, locations, updateOnly, programArguments, icon);
        }


        public void RemoveShortcutsForExecutable(string exeName, ShortcutLocation locations)
        {
            var installHelpers = new ApplyReleasesImpl(RootAppDirectory);
            installHelpers.RemoveShortcutsForExecutable(exeName, locations);
        }

        public SemanticVersion CurrentlyInstalledVersion(string executable = null)
        {
            executable = executable ??
                         Path.GetDirectoryName(typeof(PoeUpdateManager).Assembly.Location);

            if (!executable.StartsWith(RootAppDirectory, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var appDirName = executable.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .FirstOrDefault(x => x.StartsWith("app-", StringComparison.OrdinalIgnoreCase));

            return appDirName?.ToSemanticVersion();
        }

        public void Dispose()
        {
            var disp = Interlocked.Exchange(ref updateLock, null);
            disp?.Dispose();
        }

        public Dictionary<ShortcutLocation, ShellLink> GetShortcutsForExecutable(string exeName, ShortcutLocation locations, string programArguments = null)
        {
            var installHelpers = new ApplyReleasesImpl(RootAppDirectory);
            return installHelpers.GetShortcutsForExecutable(exeName, locations, programArguments);
        }

        public void KillAllExecutablesBelongingToPackage()
        {
            var installHelpers = new InstallHelperImpl(ApplicationName, RootAppDirectory);
            installHelpers.KillAllProcessesBelongingToPackage();
        }

        public static void RestartApp(string exeToStart = null, string arguments = null)
        {
            // NB: Here's how this method works:
            //
            // 1. We're going to pass the *name* of our EXE and the params to 
            //    Update.exe
            // 2. Update.exe is going to grab our PID (via getting its parent), 
            //    then wait for us to exit.
            // 3. We exit cleanly, dropping any single-instance mutexes or 
            //    whatever.
            // 4. Update.exe unblocks, then we launch the app again, possibly 
            //    launching a different version than we started with (this is why
            //    we take the app's *name* rather than a full path)

            exeToStart = exeToStart ?? Path.GetFileName(Assembly.GetEntryAssembly().Location);
            var argsArg = arguments != null
                ? $"-a \"{arguments}\""
                : "";

            Exiting = true;

            Process.Start(GetUpdateExe(), $"--processStartAndWait {exeToStart} {argsArg}");

            // NB: We have to give update.exe some time to grab our PID, but
            // we can't use WaitForInputIdle because we probably don't have
            // whatever WaitForInputIdle considers a message loop.
            Thread.Sleep(500);
            Environment.Exit(0);
        }

        public static async Task<Process> RestartAppWhenExited(string exeToStart = null, string arguments = null)
        {
            // NB: Here's how this method works:
            //
            // 1. We're going to pass the *name* of our EXE and the params to 
            //    Update.exe
            // 2. Update.exe is going to grab our PID (via getting its parent), 
            //    then wait for us to exit.
            // 3. Return control and new Process back to caller and allow them to Exit as desired.
            // 4. After our process exits, Update.exe unblocks, then we launch the app again, possibly 
            //    launching a different version than we started with (this is why
            //    we take the app's *name* rather than a full path)

            exeToStart = exeToStart ?? Path.GetFileName(Assembly.GetEntryAssembly().Location);
            var argsArg = arguments != null
                ? $"-a \"{arguments}\""
                : "";

            Exiting = true;

            var updateProcess = Process.Start(GetUpdateExe(), $"--processStartAndWait {exeToStart} {argsArg}");

            await Task.Delay(500);

            return updateProcess;
        }

        public static string GetLocalAppDataDirectory(string assemblyLocation = null)
        {
            // Try to divine our our own install location via reading tea leaves
            //
            // * We're Update.exe, running in the app's install folder
            // * We're Update.exe, running on initial install from SquirrelTemp
            // * We're a C# EXE with Squirrel linked in

            var assembly = Assembly.GetEntryAssembly();
            if (assemblyLocation == null && assembly == null)
            {
                // dunno lol
                return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            }

            assemblyLocation = assemblyLocation ?? assembly.Location;

            if (Path.GetFileName(assemblyLocation).Equals("update.exe", StringComparison.OrdinalIgnoreCase))
            {
                // NB: Both the "SquirrelTemp" case and the "App's folder" case 
                // mean that the root app dir is one up
                var oneFolderUpFromAppFolder = Path.Combine(Path.GetDirectoryName(assemblyLocation), "..");
                return Path.GetFullPath(oneFolderUpFromAppFolder);
            }

            var twoFoldersUpFromAppFolder = Path.Combine(Path.GetDirectoryName(assemblyLocation), "..\\..");
            return Path.GetFullPath(twoFoldersUpFromAppFolder);
        }

        ~PoeUpdateManager()
        {
            if (updateLock != null && !Exiting)
            {
                throw new Exception("You must dispose UpdateManager!");
            }
        }

        private Task<IDisposable> AcquireUpdateLock()
        {
            if (updateLock != null)
            {
                return Task.FromResult(updateLock);
            }

            return Task.Run(
                () =>
                {
                    var key = Utility.CalculateStreamSha1(new MemoryStream(Encoding.UTF8.GetBytes(RootAppDirectory)));

                    IDisposable theLock;
                    try
                    {
                        theLock = ModeDetector.InUnitTestRunner()
                            ? Disposable.Create(() => { })
                            : new SingleGlobalInstance(key, TimeSpan.FromMilliseconds(2000));
                    }
                    catch (TimeoutException)
                    {
                        throw new TimeoutException("Couldn't acquire update lock, another instance may be running updates");
                    }

                    var ret = Disposable.Create(
                        () =>
                        {
                            theLock.Dispose();
                            updateLock = null;
                        });

                    updateLock = ret;
                    return ret;
                });
        }

        private static string GetApplicationName()
        {
            var fi = new FileInfo(GetUpdateExe());
            return fi.Directory.Name;
        }

        private static string GetUpdateExe()
        {
            var assembly = Assembly.GetEntryAssembly();

            // Are we update.exe?
            if (assembly != null &&
                Path.GetFileName(assembly.Location).Equals("update.exe", StringComparison.OrdinalIgnoreCase) &&
                assembly.Location.IndexOf("app-", StringComparison.OrdinalIgnoreCase) == -1 &&
                assembly.Location.IndexOf("SquirrelTemp", StringComparison.OrdinalIgnoreCase) == -1)
            {
                return Path.GetFullPath(assembly.Location);
            }

            assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

            var updateDotExe = Path.Combine(Path.GetDirectoryName(assembly.Location), "..\\Update.exe");
            var target = new FileInfo(updateDotExe);

            if (!target.Exists)
            {
                throw new Exception("Update.exe not found, not a Squirrel-installed app?");
            }

            return target.FullName;
        }
    }
}