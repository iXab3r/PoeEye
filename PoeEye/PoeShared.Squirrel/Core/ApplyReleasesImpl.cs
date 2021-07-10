using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.Win32;
using NuGet;
using PoeShared.Squirrel.Scaffolding;
using Splat;
using Squirrel;
using Squirrel.Shell;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using ReleasePackage = PoeShared.Squirrel.Scaffolding.ReleasePackage;

namespace PoeShared.Squirrel.Core
{
    internal class ApplyReleasesImpl : IEnableLogger
    {
        private static readonly IFluentLog Log = typeof(ApplyReleasesImpl).PrepareLogger();
            
        private readonly string rootAppDirectory;

        public ApplyReleasesImpl(string rootAppDirectory)
        {
            this.rootAppDirectory = rootAppDirectory;
        }

        public async Task<string> ApplyReleases(
            IPoeUpdateInfo updateInfo,
            bool silentInstall, 
            bool attemptingFullInstall, 
            Action<int> progress = null)
        {
            progress ??= _ => { };

            progress(5);
            var release = await CreateFullPackagesFromDeltas(updateInfo.ReleasesToApply.ToArray(), updateInfo.CurrentlyInstalledVersion);
            progress(10);

            if (release == null)
            {
                if (attemptingFullInstall)
                {
                    Log.Info("No release to install, running the app");
                    await InvokePostInstall(updateInfo.CurrentlyInstalledVersion?.Version, false, true, silentInstall);
                }

                progress(100);
                return GetDirectoryForRelease(updateInfo.CurrentlyInstalledVersion?.Version).FullName;
            }

            var ret = await Log.ErrorIfThrows(
                () => InstallPackageToAppDir(updateInfo, release),
                "Failed to install package to app dir");
            progress(30);

            var currentReleases = await Log.ErrorIfThrows(
                UpdateLocalReleasesFile,
                "Failed to update local releases file");
            progress(50);

            var newVersion = currentReleases.OrderByDescending(x => x.Version).First().Version;
            ExecuteSelfUpdate(newVersion);

            await Log.ErrorIfThrows(
                () => InvokePostInstall(newVersion, attemptingFullInstall, false, silentInstall),
                "Failed to invoke post-install");
            progress(75);

            Log.Info("Starting fixPinnedExecutables");
            Log.ErrorIfThrows(() => FixPinnedExecutables(updateInfo.FutureReleaseEntry.Version));

            Log.Info("Fixing up tray icons");

            var trayFixer = new TrayStateChanger();
            var appDir = new DirectoryInfo(Utility.AppDirForRelease(rootAppDirectory, updateInfo.FutureReleaseEntry));
            var allExes = appDir.GetFiles("*.exe").Select(x => x.Name).ToList();

            Log.ErrorIfThrows(() => trayFixer.RemoveDeadEntries(allExes, rootAppDirectory, updateInfo.FutureReleaseEntry.Version.ToString()));
            progress(80);

            UnshimOurselves();
            progress(85);

            try
            {
                var currentVersion = updateInfo.CurrentlyInstalledVersion?.Version;

                await CleanDeadVersions(currentVersion, newVersion);
            }
            catch (Exception ex)
            {
                Log.Warn("Failed to clean dead versions, continuing anyways", ex);
            }

            progress(100);

            return ret;
        }

        public async Task FullUninstall()
        {
            var currentRelease = GetReleases().MaxBy(x => x.Name.ToSemanticVersion()).First();

            Log.Info("Starting full uninstall");
            if (currentRelease.Exists)
            {
                var version = currentRelease.Name.ToSemanticVersion();

                try
                {
                    var squirrelAwareApps = SquirrelAwareExecutableDetector.GetAllSquirrelAwareApps(currentRelease.FullName);

                    if (IsAppFolderDead(currentRelease.FullName))
                    {
                        throw new Exception("App folder is dead, but we're trying to uninstall it?");
                    }

                    var allApps = currentRelease.EnumerateFiles()
                        .Where(x => x.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        .Where(
                            x => !x.Name.StartsWith("squirrel.", StringComparison.OrdinalIgnoreCase) &&
                                 !x.Name.StartsWith("update.", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (squirrelAwareApps.Count > 0)
                    {
                        await squirrelAwareApps.ForEachAsync(
                            async exe =>
                            {
                                using (var cts = new CancellationTokenSource())
                                {
                                    cts.CancelAfter(10 * 1000);

                                    try
                                    {
                                        await Utility.InvokeProcessAsync(exe, $"--squirrel-uninstall {version}", cts.Token);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error("Failed to run cleanup hook, continuing: " + exe, ex);
                                    }
                                }
                            },
                            1 /*at a time*/);
                    }
                    else
                    {
                        allApps.ForEach(x => RemoveShortcutsForExecutable(x.Name, ShortcutLocation.StartMenu | ShortcutLocation.Desktop));
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn("Failed to run pre-uninstall hooks, uninstalling anyways", ex);
                }
            }

            try
            {
                Log.ErrorIfThrows(() => FixPinnedExecutables(new SemanticVersion(255, 255, 255, 255), true));
            }
            catch
            {
            }

            await Log.ErrorIfThrows(
                () => Utility.DeleteDirectoryOrJustGiveUp(rootAppDirectory),
                "Failed to delete app directory: " + rootAppDirectory);

            // NB: We drop this file here so that --checkInstall will ignore 
            // this folder - if we don't do this, users who "accidentally" run as 
            // administrator will find the app reinstalling itself on every
            // reboot
            if (!Directory.Exists(rootAppDirectory))
            {
                Directory.CreateDirectory(rootAppDirectory);
            }

            File.WriteAllText(Path.Combine(rootAppDirectory, ".dead"), " ");
        }

        public Dictionary<ShortcutLocation, ShellLink> GetShortcutsForExecutable(string exeName, ShortcutLocation locations, string programArguments)
        {
            Log.Info($"About to create shortcuts for {exeName}, rootAppDir {rootAppDirectory}");

            var releases = Utility.LoadLocalReleases(Utility.LocalReleaseFileForAppDir(rootAppDirectory));
            var thisRelease = Utility.FindCurrentVersion(releases);

            var zf = new ZipPackage(
                Path.Combine(
                    Utility.PackageDirectoryForAppDir(rootAppDirectory),
                    thisRelease.Filename));

            var exePath = Path.Combine(Utility.AppDirForRelease(rootAppDirectory, thisRelease), exeName);
            var fileVerInfo = FileVersionInfo.GetVersionInfo(exePath);

            var ret = new Dictionary<ShortcutLocation, ShellLink>();
            foreach (var f in (ShortcutLocation[]) Enum.GetValues(typeof(ShortcutLocation)))
            {
                if (!locations.HasFlag(f))
                {
                    continue;
                }

                var file = LinkTargetForVersionInfo(f, zf, fileVerInfo);
                var appUserModelId = $"com.squirrel.{zf.Id.Replace(" ", "")}.{exeName.Replace(".exe", "").Replace(" ", "")}";
                var toastActivatorClsdid = Utility.CreateGuidFromHash(appUserModelId).ToString();

                Log.Info($"Creating shortcut for {exeName} => {file}");
                Log.Info($"appUserModelId: {appUserModelId} | toastActivatorCLSID: {toastActivatorClsdid}");

                var target = Path.Combine(rootAppDirectory, exeName);
                var sl = new ShellLink
                {
                    Target = target,
                    IconPath = target,
                    IconIndex = 0,
                    WorkingDirectory = Path.GetDirectoryName(exePath),
                    Description = zf.Description
                };

                if (!string.IsNullOrWhiteSpace(programArguments))
                {
                    sl.Arguments += $" -a \"{programArguments}\"";
                }

                sl.SetAppUserModelId(appUserModelId);
                sl.SetToastActivatorCLSID(toastActivatorClsdid);

                ret.Add(f, sl);
            }

            return ret;
        }

        public void CreateShortcutsForExecutable(string exeName, ShortcutLocation locations, bool updateOnly, string programArguments, string icon)
        {
            Log.Info($"About to create shortcuts for {exeName}, rootAppDir {rootAppDirectory}");

            var releases = Utility.LoadLocalReleases(Utility.LocalReleaseFileForAppDir(rootAppDirectory));
            var thisRelease = Utility.FindCurrentVersion(releases);

            var zf = new ZipPackage(
                Path.Combine(
                    Utility.PackageDirectoryForAppDir(rootAppDirectory),
                    thisRelease.Filename));

            var exePath = Path.Combine(Utility.AppDirForRelease(rootAppDirectory, thisRelease), exeName);
            var fileVerInfo = FileVersionInfo.GetVersionInfo(exePath);

            foreach (var f in (ShortcutLocation[]) Enum.GetValues(typeof(ShortcutLocation)))
            {
                if (!locations.HasFlag(f))
                {
                    continue;
                }

                var file = LinkTargetForVersionInfo(f, zf, fileVerInfo);
                var fileExists = File.Exists(file);

                // NB: If we've already installed the app, but the shortcut
                // is no longer there, we have to assume that the user didn't
                // want it there and explicitly deleted it, so we shouldn't
                // annoy them by recreating it.
                if (!fileExists && updateOnly)
                {
                    Log.Warn($"Wanted to update shortcut {file} but it appears user deleted it");
                    continue;
                }

                Log.Info($"Creating shortcut for {exeName} => {file}");

                ShellLink sl;
                Log.ErrorIfThrows(
                    () => Utility.Retry(
                        () =>
                        {
                            File.Delete(file);

                            var target = Path.Combine(rootAppDirectory, exeName);
                            sl = new ShellLink
                            {
                                Target = target,
                                IconPath = icon ?? target,
                                IconIndex = 0,
                                WorkingDirectory = Path.GetDirectoryName(exePath),
                                Description = zf.Description
                            };

                            if (!string.IsNullOrWhiteSpace(programArguments))
                            {
                                sl.Arguments += $" -a \"{programArguments}\"";
                            }

                            var appUserModelId = $"com.squirrel.{zf.Id.Replace(" ", "")}.{exeName.Replace(".exe", "").Replace(" ", "")}";
                            var toastActivatorClsid = Utility.CreateGuidFromHash(appUserModelId).ToString();

                            sl.SetAppUserModelId(appUserModelId);
                            sl.SetToastActivatorCLSID(toastActivatorClsid);

                            Log.Info($"About to save shortcut: {file} (target {sl.Target}, workingDir {sl.WorkingDirectory}, args {sl.Arguments}, toastActivatorCSLID {toastActivatorClsid})");
                            if (ModeDetector.InUnitTestRunner() == false)
                            {
                                sl.Save(file);
                            }
                        },
                        4),
                    "Can't write shortcut: " + file);
            }

            FixPinnedExecutables(zf.Version);
        }

        public void RemoveShortcutsForExecutable(string exeName, ShortcutLocation locations)
        {
            var releases = Utility.LoadLocalReleases(Utility.LocalReleaseFileForAppDir(rootAppDirectory));
            var thisRelease = Utility.FindCurrentVersion(releases);

            var zf = new ZipPackage(
                Path.Combine(
                    Utility.PackageDirectoryForAppDir(rootAppDirectory),
                    thisRelease.Filename));

            var fileVerInfo = FileVersionInfo.GetVersionInfo(
                Path.Combine(Utility.AppDirForRelease(rootAppDirectory, thisRelease), exeName));

            foreach (var f in (ShortcutLocation[]) Enum.GetValues(typeof(ShortcutLocation)))
            {
                if (!locations.HasFlag(f))
                {
                    continue;
                }

                var file = LinkTargetForVersionInfo(f, zf, fileVerInfo);

                Log.Info($"Removing shortcut for {exeName} => {file}");

                Log.ErrorIfThrows(
                    () =>
                    {
                        if (File.Exists(file))
                        {
                            File.Delete(file);
                        }
                    },
                    "Couldn't delete shortcut: " + file);
            }

            FixPinnedExecutables(zf.Version);
        }

        private Task<string> InstallPackageToAppDir(IPoeUpdateInfo updateInfo, IReleaseEntry release)
        {
            return Task.Run(
                async () =>
                {
                    var target = GetDirectoryForRelease(release.Version);

                    // NB: This might happen if we got killed partially through applying the release
                    if (target.Exists)
                    {
                        Log.Warn("Found partially applied release folder, killing it: " + target.FullName);
                        await Utility.DeleteDirectory(target.FullName);
                    }

                    target.Create();

                    Log.Info($"Writing files to app directory: {target.FullName}");
                    await ReleasePackage.ExtractZipForInstall(
                        Path.Combine(updateInfo.PackageDirectory, release.Filename),
                        target.FullName,
                        rootAppDirectory);

                    return target.FullName;
                });
        }

        private async Task<IReleaseEntry> CreateFullPackagesFromDeltas(IReleaseEntry[] releasesToApply, IReleaseEntry currentVersion)
        {
            Guard.ArgumentIsTrue(releasesToApply != null, "releasesToApply != null");

            // If there are no remote releases at all, bail
            if (releasesToApply == null || !releasesToApply.Any())
            {
                Log.Debug("Releases to apply is empty");
                return null;
            }
                
            if (currentVersion == null)
            {
                Log.Debug("Current version is not set, skipping delta-phase");
                return null;
            }
                
            Log.Debug($"Applying delta-releases to {new { currentVersion?.PackageName, currentVersion?.Version }}, chain: {string.Join(" => ", releasesToApply.Select(x => new { x.Filename, x.Version, x.IsDelta }))}");
            // If there are no deltas in our list, we're already done
            if (releasesToApply.All(x => !x.IsDelta))
            {
                return releasesToApply.MaxBy(x => x.Version).FirstOrDefault();
            }

            if (!releasesToApply.All(x => x.IsDelta))
            {
                throw new Exception("Cannot apply combinations of delta and full packages");
            }

            // Smash together our base full package and the nearest delta
            var ret = await Task.Run(
                () =>
                {
                    var basePkg = new ReleasePackage(Path.Combine(rootAppDirectory, "packages", currentVersion.Filename));
                    var deltaPkg = new ReleasePackage(Path.Combine(rootAppDirectory, "packages", releasesToApply.First().Filename));

                    using var tempDirectoryAnchor = Utility.WithTempDirectory(out var parentDirectory, rootAppDirectory);
                    Log.Debug($"Preparing delta-package in {parentDirectory}, base: {basePkg}, delta: {deltaPkg}");
                    var deltaBuilder = new DeltaPackageBuilder(parentDirectory);

                    var finalPkgPath = Regex.Replace(deltaPkg.InputPackageFile, @"-delta.nupkg$", ".nupkg", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    if (File.Exists(finalPkgPath))
                    {
                        Log.Debug($"Final package already exists @ {finalPkgPath}, removing it");
                        File.Delete(finalPkgPath);
                    }
                        
                    return deltaBuilder.ApplyDeltaPackage(
                        basePkg,
                        deltaPkg,
                        finalPkgPath);
                });

            if (releasesToApply.Count() == 1)
            {
                return ReleaseEntry.GenerateFromFile(ret.InputPackageFile);
            }

            var fi = new FileInfo(ret.InputPackageFile);
            var entry = ReleaseEntry.GenerateFromFile(fi.OpenRead(), fi.Name);

            // Recursively combine the rest of them
            return await CreateFullPackagesFromDeltas(releasesToApply.Skip(1).ToArray(), entry);
        }

        private void ExecuteSelfUpdate(SemanticVersion currentVersion)
        {
            var targetDir = GetDirectoryForRelease(currentVersion);
            var newSquirrel = Path.Combine(targetDir.FullName, "Squirrel.exe");
            if (!File.Exists(newSquirrel))
            {
                return;
            }

            // If we're running in the context of Update.exe, we can't 
            // update ourselves. Instead, ask the new Update.exe to do it
            // once we exit
            var us = Assembly.GetEntryAssembly();
            if (us != null && Path.GetFileName(us.Location).Equals("update.exe", StringComparison.OrdinalIgnoreCase))
            {
                var appName = targetDir.Parent.Name;

                Process.Start(newSquirrel, "--updateSelf=" + us.Location);
                return;
            }

            // If we're *not* Update.exe, this is easy, it's just a file copy
            Utility.Retry(
                () =>
                    File.Copy(newSquirrel, Path.Combine(targetDir.Parent.FullName, "Update.exe"), true));
        }

        private async Task InvokePostInstall(SemanticVersion currentVersion, bool isInitialInstall, bool firstRunOnly, bool silentInstall)
        {
            var targetDir = GetDirectoryForRelease(currentVersion);
            var args = isInitialInstall
                ? $"--squirrel-install {currentVersion}"
                : $"--squirrel-updated {currentVersion}";

            var squirrelApps = SquirrelAwareExecutableDetector.GetAllSquirrelAwareApps(targetDir.FullName);

            Log.Info($"Squirrel Enabled Apps: [{string.Join(",", squirrelApps)}]");

            // For each app, run the install command in-order and wait
            if (!firstRunOnly)
            {
                await squirrelApps.ForEachAsync(
                    async exe =>
                    {
                        using (var cts = new CancellationTokenSource())
                        {
                            cts.CancelAfter(15 * 1000);

                            try
                            {
                                await Utility.InvokeProcessAsync(exe, args, cts.Token);
                            }
                            catch (Exception ex)
                            {
                                Log.Error("Couldn't run Squirrel hook, continuing: " + exe, ex);
                            }
                        }
                    },
                    1 /* at a time */);
            }

            // If this is the first run, we run the apps with first-run and 
            // *don't* wait for them, since they're probably the main EXE
            if (squirrelApps.Count == 0)
            {
                Log.Warn("No apps are marked as Squirrel-aware! Going to run them all");

                squirrelApps = targetDir.EnumerateFiles()
                    .Where(x => x.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    .Where(x => !x.Name.StartsWith("squirrel.", StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.FullName)
                    .ToList();

                // Create shortcuts for apps automatically if they didn't
                // create any Squirrel-aware apps
                squirrelApps.ForEach(
                    x => CreateShortcutsForExecutable(
                        Path.GetFileName(x),
                        ShortcutLocation.Desktop | ShortcutLocation.StartMenu,
                        isInitialInstall == false,
                        null,
                        null));
            }

            if (!isInitialInstall || silentInstall)
            {
                return;
            }

            var firstRunParam = isInitialInstall
                ? "--squirrel-firstrun"
                : "";
            squirrelApps
                .Select(exe => new ProcessStartInfo(exe, firstRunParam) {WorkingDirectory = Path.GetDirectoryName(exe)})
                .ForEach(info => Process.Start(info));
        }

        private void FixPinnedExecutables(SemanticVersion newCurrentVersion, bool removeAll = false)
        {
            if (Environment.OSVersion.Version < new Version(6, 1))
            {
                Log.Warn($"fixPinnedExecutables: Found OS Version '{Environment.OSVersion.VersionString}', exiting...");
                return;
            }

            var newCurrentFolder = "app-" + newCurrentVersion;
            var newAppPath = Path.Combine(rootAppDirectory, newCurrentFolder);

            var taskbarPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Microsoft\\Internet Explorer\\Quick Launch\\User Pinned\\TaskBar");

            if (!Directory.Exists(taskbarPath))
            {
                Log.Info("fixPinnedExecutables: PinnedExecutables directory doesn't exitsts, skiping...");
                return;
            }

            var resolveLink = new Func<FileInfo, ShellLink>(
                file =>
                {
                    try
                    {
                        Log.Info("Examining Pin: " + file);
                        return new ShellLink(file.FullName);
                    }
                    catch (Exception ex)
                    {
                        var message = $"File '{file.FullName}' could not be converted into a valid ShellLink";
                        Log.Warn(message, ex);
                        return null;
                    }
                });

            var shellLinks = new DirectoryInfo(taskbarPath).GetFiles("*.lnk").Select(resolveLink).ToArray();

            foreach (var shortcut in shellLinks)
            {
                try
                {
                    if (shortcut == null)
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(shortcut.Target))
                    {
                        continue;
                    }

                    if (!shortcut.Target.StartsWith(rootAppDirectory, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (removeAll)
                    {
                        Utility.DeleteFileHarder(shortcut.ShortCutFile);
                    }
                    else
                    {
                        UpdateLink(shortcut, newAppPath);
                    }
                }
                catch (Exception ex)
                {
                    var message = $"fixPinnedExecutables: shortcut failed: {shortcut.Target}";
                    Log.Error(message, ex);
                }
            }
        }

        private void UpdateLink(ShellLink shortcut, string newAppPath)
        {
            Log.Info($"Processing shortcut '{shortcut.ShortCutFile}'");

            var target = Environment.ExpandEnvironmentVariables(shortcut.Target);
            var targetIsUpdateDotExe = target.EndsWith("update.exe", StringComparison.OrdinalIgnoreCase);

            Log.Info($"Old shortcut target: '{target}'");

            // NB: In 1.5.0 we accidentally fixed the target of pinned shortcuts but left the arguments,
            // so if we find a shortcut with --processStart in the args, we're gonna stomp it even though
            // what we _should_ do is stomp it only if the target is Update.exe
            if (shortcut.Arguments.Contains("--processStart"))
            {
                shortcut.Arguments = "";
            }

            if (!targetIsUpdateDotExe)
            {
                target = Path.Combine(rootAppDirectory, Path.GetFileName(shortcut.Target));
            }
            else
            {
                target = Path.Combine(rootAppDirectory, Path.GetFileName(shortcut.IconPath));
            }

            Log.Info($"New shortcut target: '{target}'");

            shortcut.WorkingDirectory = newAppPath;
            shortcut.Target = target;

            Log.Info($"Old iconPath is: '{shortcut.IconPath}'");
            shortcut.IconPath = target;
            shortcut.IconIndex = 0;

            Log.ErrorIfThrows(() => Utility.Retry(() => shortcut.Save()), "Couldn't write shortcut " + shortcut.ShortCutFile);
            Log.Info("Finished shortcut successfully");
        }

        internal void UnshimOurselves()
        {
            new[] {RegistryView.Registry32, RegistryView.Registry64}.ForEach(
                view =>
                {
                    var baseKey = default(RegistryKey);
                    var regKey = default(RegistryKey);

                    try
                    {
                        baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, view);
                        regKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers");

                        if (regKey == null)
                        {
                            return;
                        }

                        var toDelete = regKey.GetValueNames()
                            .Where(x => x.StartsWith(rootAppDirectory, StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        toDelete.ForEach(
                            x =>
                                Log.LogIfThrows(
                                    LogLevel.Warn,
                                    "Failed to delete key: " + x,
                                    () => regKey.DeleteValue(x)));
                    }
                    catch (Exception e)
                    {
                        Log.Warn("Couldn't rewrite shim RegKey, most likely no apps are shimmed", e);
                    }
                    finally
                    {
                        regKey?.Dispose();

                        baseKey?.Dispose();
                    }
                });
        }

        // NB: Once we uninstall the old version of the app, we try to schedule
        // it to be deleted at next reboot. Unfortunately, depending on whether
        // the user has admin permissions, this can fail. So as a failsafe,
        // before we try to apply any update, we assume previous versions in the
        // directory are "dead" (i.e. already uninstalled, but not deleted), and
        // we blow them away. This is to make sure that we don't attempt to run
        // an uninstaller on an already-uninstalled version.
        private async Task CleanDeadVersions(SemanticVersion originalVersion, SemanticVersion currentVersion, bool forceUninstall = false)
        {
            if (currentVersion == null)
            {
                return;
            }

            var di = new DirectoryInfo(rootAppDirectory);
            if (!di.Exists)
            {
                return;
            }

            Log.Info($"cleanDeadVersions: for version {currentVersion}");

            string originalVersionFolder = null;
            if (originalVersion != null)
            {
                originalVersionFolder = GetDirectoryForRelease(originalVersion).Name;
                Log.Info($"cleanDeadVersions: exclude folder {originalVersionFolder}");
            }

            string currentVersionFolder = null;
            if (currentVersion != null)
            {
                currentVersionFolder = GetDirectoryForRelease(currentVersion).Name;
                Log.Info($"cleanDeadVersions: exclude folder {currentVersionFolder}");
            }

            // NB: If we try to access a directory that has already been 
            // scheduled for deletion by MoveFileEx it throws what seems like
            // NT's only error code, ERROR_ACCESS_DENIED. Squelch errors that
            // come from here.
            var toCleanup = di.GetDirectories()
                .Where(x => x.Name.ToLowerInvariant().Contains("app-"))
                .Where(x => x.Name != currentVersionFolder && x.Name != originalVersionFolder)
                .Where(x => !IsAppFolderDead(x.FullName));

            if (forceUninstall == false)
            {
                await toCleanup.ForEachAsync(
                    async x =>
                    {
                        var squirrelApps = SquirrelAwareExecutableDetector.GetAllSquirrelAwareApps(x.FullName);
                        var args = $"--squirrel-obsolete {x.Name.Replace("app-", "")}";

                        if (squirrelApps.Count > 0)
                        {
                            // For each app, run the install command in-order and wait
                            await squirrelApps.ForEachAsync(
                                async exe =>
                                {
                                    using (var cts = new CancellationTokenSource())
                                    {
                                        cts.CancelAfter(10 * 1000);

                                        try
                                        {
                                            await Utility.InvokeProcessAsync(exe, args, cts.Token);
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Error("Coudln't run Squirrel hook, continuing: " + exe, ex);
                                        }
                                    }
                                },
                                1 /* at a time */);
                        }
                    });
            }

            // Include dead folders in folders to :fire:
            toCleanup = di.GetDirectories()
                .Where(x => x.Name.ToLowerInvariant().Contains("app-"))
                .Where(x => x.Name != currentVersionFolder && x.Name != originalVersionFolder);

            // Get the current process list in an attempt to not burn 
            // directories which have running processes
            var runningProcesses = UnsafeUtility.EnumerateProcesses();

            // Finally, clean up the app-X.Y.Z directories
            await toCleanup.ForEachAsync(
                async x =>
                {
                    try
                    {
                        if (runningProcesses.All(p => p.Item1 == null || !p.Item1.StartsWith(x.FullName, StringComparison.OrdinalIgnoreCase)))
                        {
                            await Utility.DeleteDirectoryOrJustGiveUp(x.FullName);
                        }

                        if (Directory.Exists(x.FullName))
                        {
                            // NB: If we cannot clean up a directory, we need to make 
                            // sure that anyone finding it later won't attempt to run
                            // Squirrel events on it. We'll mark it with a .dead file
                            MarkAppFolderAsDead(x.FullName);
                        }
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Log.Warn("Couldn't delete directory: " + x.FullName, ex);

                        // NB: Same deal as above
                        MarkAppFolderAsDead(x.FullName);
                    }
                });

            // Clean up the packages directory too
            var releasesFile = Utility.LocalReleaseFileForAppDir(rootAppDirectory);
            var entries = ReleaseEntry.ParseReleaseFile(File.ReadAllText(releasesFile, Encoding.UTF8));
            var pkgDir = Utility.PackageDirectoryForAppDir(rootAppDirectory);
            var releaseEntry = default(ReleaseEntry);

            foreach (var entry in entries)
            {
                if (entry.Version == currentVersion)
                {
                    releaseEntry = ReleaseEntry.GenerateFromFile(Path.Combine(pkgDir, entry.Filename));
                    continue;
                }

                File.Delete(Path.Combine(pkgDir, entry.Filename));
            }

            ReleaseEntry.WriteReleaseFile(new[] {releaseEntry}, releasesFile);
        }

        private static void MarkAppFolderAsDead(string appFolderPath)
        {
            File.WriteAllText(Path.Combine(appFolderPath, ".dead"), "");
        }

        private static bool IsAppFolderDead(string appFolderPath)
        {
            return File.Exists(Path.Combine(appFolderPath, ".dead"));
        }

        internal async Task<List<ReleaseEntry>> UpdateLocalReleasesFile()
        {
            return await Task.Run(() => ReleaseEntry.BuildReleasesFile(Utility.PackageDirectoryForAppDir(rootAppDirectory)));
        }

        private IEnumerable<DirectoryInfo> GetReleases()
        {
            var rootDirectory = new DirectoryInfo(rootAppDirectory);

            if (!rootDirectory.Exists)
            {
                return Enumerable.Empty<DirectoryInfo>();
            }

            return rootDirectory.GetDirectories()
                .Where(x => x.Name.StartsWith("app-", StringComparison.InvariantCultureIgnoreCase));
        }

        private DirectoryInfo GetDirectoryForRelease(SemanticVersion releaseVersion)
        {
            return new DirectoryInfo(Path.Combine(rootAppDirectory, "app-" + releaseVersion));
        }

        private string LinkTargetForVersionInfo(ShortcutLocation location, IPackage package, FileVersionInfo versionInfo)
        {
            var possibleProductNames = new[]
            {
                versionInfo.ProductName,
                package.Title,
                versionInfo.FileDescription,
                Path.GetFileNameWithoutExtension(versionInfo.FileName)
            };

            var possibleCompanyNames = new[]
            {
                versionInfo.CompanyName,
                package.Authors.FirstOrDefault() ?? package.Id
            };

            var prodName = possibleCompanyNames.First(x => !string.IsNullOrWhiteSpace(x));
            var pkgName = possibleProductNames.First(x => !string.IsNullOrWhiteSpace(x));

            return GetLinkTarget(location, pkgName, prodName);
        }

        private string GetLinkTarget(ShortcutLocation location, string title, string applicationName, bool createDirectoryIfNecessary = true)
        {
            var dir = default(string);

            switch (location)
            {
                case ShortcutLocation.Desktop:
                    dir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    break;
                case ShortcutLocation.StartMenu:
                    dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", applicationName);
                    break;
                case ShortcutLocation.Startup:
                    dir = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                    break;
                case ShortcutLocation.AppRoot:
                    dir = rootAppDirectory;
                    break;
            }

            if (createDirectoryIfNecessary && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            return Path.Combine(dir, title + ".lnk");
        }
    }
}