﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Microsoft.Win32;
using NuGet;
using PoeShared.Squirrel.Scaffolding;
using Splat;
using Squirrel;

namespace PoeShared.Squirrel.Core
{
    public sealed partial class PoeUpdateManager
    {
        internal class InstallHelperImpl : IEnableLogger
        {
            private static readonly ILog Log = LogManager.GetLogger(typeof(InstallHelperImpl));

            private const string CurrentVersionRegSubKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
            private const string UninstallRegSubKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
            private readonly string applicationName;
            private readonly string rootAppDirectory;

            public InstallHelperImpl(string applicationName, string rootAppDirectory)
            {
                this.applicationName = applicationName;
                this.rootAppDirectory = rootAppDirectory;
            }

            public async Task<RegistryKey> CreateUninstallerRegistryEntry(string uninstallCmd, string quietSwitch)
            {
                var releaseContent = File.ReadAllText(Path.Combine(rootAppDirectory, "packages", "RELEASES"), Encoding.UTF8);
                var releases = ReleaseEntry.ParseReleaseFile(releaseContent);
                var latest = releases.Where(x => !x.IsDelta).OrderByDescending(x => x.Version).First();

                // Download the icon and PNG => ICO it. If this doesn't work, who cares
                var pkgPath = Path.Combine(rootAppDirectory, "packages", latest.Filename);
                var zp = new ZipPackage(pkgPath);

                var targetPng = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png");
                var targetIco = Path.Combine(rootAppDirectory, "app.ico");

                // NB: Sometimes the Uninstall key doesn't exist
                using (var parentKey =
                    RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default)
                        .CreateSubKey("Uninstall", RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    ;
                }

                var key = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default)
                    .CreateSubKey(UninstallRegSubKey + "\\" + applicationName, RegistryKeyPermissionCheck.ReadWriteSubTree);

                if (zp.IconUrl != null && !File.Exists(targetIco))
                {
                    try
                    {
                        using (var wc = Utility.CreateWebClient())
                        {
                            await wc.DownloadFileTaskAsync(zp.IconUrl, targetPng);
                            using (var fs = new FileStream(targetIco, FileMode.Create))
                            {
                                if (zp.IconUrl.AbsolutePath.EndsWith("ico"))
                                {
                                    var bytes = File.ReadAllBytes(targetPng);
                                    fs.Write(bytes, 0, bytes.Length);
                                }
                                else
                                {
                                    using (var bmp = (Bitmap) Image.FromFile(targetPng))
                                    using (var ico = Icon.FromHandle(bmp.GetHicon()))
                                    {
                                        ico.Save(fs);
                                    }
                                }

                                key.SetValue("DisplayIcon", targetIco, RegistryValueKind.String);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Info("Couldn't write uninstall icon, don't care", ex);
                    }
                    finally
                    {
                        File.Delete(targetPng);
                    }
                }

                var stringsToWrite = new[]
                {
                    new {Key = "DisplayName", Value = zp.Title ?? zp.Description ?? zp.Summary},
                    new {Key = "DisplayVersion", Value = zp.Version.ToString()},
                    new {Key = "InstallDate", Value = DateTime.Now.ToString("yyyyMMdd")},
                    new {Key = "InstallLocation", Value = rootAppDirectory},
                    new {Key = "Publisher", Value = string.Join(",", zp.Authors)},
                    new {Key = "QuietUninstallString", Value = $"{uninstallCmd} {quietSwitch}"},
                    new {Key = "UninstallString", Value = uninstallCmd},
                    new
                    {
                        Key = "URLUpdateInfo", Value = zp.ProjectUrl != null
                            ? zp.ProjectUrl.ToString()
                            : ""
                    }
                };

                var dwordsToWrite = new[]
                {
                    new {Key = "EstimatedSize", Value = (int) (new FileInfo(pkgPath).Length / 1024)},
                    new {Key = "NoModify", Value = 1},
                    new {Key = "NoRepair", Value = 1},
                    new {Key = "Language", Value = 0x0409}
                };

                foreach (var kvp in stringsToWrite)
                {
                    key.SetValue(kvp.Key, kvp.Value, RegistryValueKind.String);
                }

                foreach (var kvp in dwordsToWrite)
                {
                    key.SetValue(kvp.Key, kvp.Value, RegistryValueKind.DWord);
                }

                return key;
            }

            public void KillAllProcessesBelongingToPackage()
            {
                var ourExe = Assembly.GetEntryAssembly();
                var ourExePath = ourExe != null
                    ? ourExe.Location
                    : null;

                UnsafeUtility.EnumerateProcesses()
                    .Where(
                        x =>
                        {
                            // Processes we can't query will have an empty process name, we can't kill them
                            // anyways
                            if (string.IsNullOrWhiteSpace(x.Item1))
                            {
                                return false;
                            }

                            // Files that aren't in our root app directory are untouched
                            if (!x.Item1.StartsWith(rootAppDirectory, StringComparison.OrdinalIgnoreCase))
                            {
                                return false;
                            }

                            // Never kill our own EXE
                            if (ourExePath != null && x.Item1.Equals(ourExePath, StringComparison.OrdinalIgnoreCase))
                            {
                                return false;
                            }

                            var name = Path.GetFileName(x.Item1).ToLowerInvariant();
                            if (name == "squirrel.exe" || name == "update.exe")
                            {
                                return false;
                            }

                            return true;
                        })
                    .ForEach(
                        x =>
                        {
                            try
                            {
                                Log.WarnIfThrows(() => Process.GetProcessById(x.Item2).Kill());
                            }
                            catch
                            {
                            }
                        });
            }

            public Task<RegistryKey> CreateUninstallerRegistryEntry()
            {
                var updateDotExe = Path.Combine(rootAppDirectory, "Update.exe");
                return CreateUninstallerRegistryEntry($"\"{updateDotExe}\" --uninstall", "-s");
            }

            public void RemoveUninstallerRegistryEntry()
            {
                var key = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default)
                    .OpenSubKey(UninstallRegSubKey, true);
                key.DeleteSubKeyTree(applicationName);
            }
        }
    }
}