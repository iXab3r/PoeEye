using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Squirrel;

namespace PoeShared.Squirrel.Core;

public interface IPoeUpdateManager : IDisposable
{
    Task<IPoeUpdateInfo> CheckForUpdate(bool ignoreDeltaUpdates, Action<int> progress = null);
    Task<IPoeUpdateInfo> CheckForUpdate(Version targetVersion, Action<int> progress = null);
    Task<IReadOnlyCollection<FileInfo>> DownloadReleases(IReadOnlyCollection<IReleaseEntry> releasesToDownload, Action<int> progress = null);
    Task<bool> VerifyReleases(IReadOnlyCollection<IReleaseEntry> releasesToDownload, Action<int> progress = null);
    Task<string> ApplyReleases(IPoeUpdateInfo updateInfo, Action<int> progress = null);
    Task<IPoeUpdateInfo> PrepareUpdate(bool ignoreDeltaUpdates, IReadOnlyCollection<IReleaseEntry> localReleases, IReadOnlyCollection<IReleaseEntry> remoteReleases);
}