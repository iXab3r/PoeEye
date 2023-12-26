using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using PoeShared.Services;
using Squirrel;

namespace PoeShared.Squirrel.Core;

public sealed class ResilientUpdateManager : DisposableReactiveObject, IPoeUpdateManager
{
    private static readonly IFluentLog Log = typeof(ResilientUpdateManager).PrepareLogger();

    private readonly Func<string, Task<IPoeUpdateManager>> managerFactory;
    private readonly ResourceChooser<string> uriChooser;
    private readonly NamedLock gate = new("ManagerLock");

    public ResilientUpdateManager(
        IEnumerable<string> uris,
        Func<string, Task<IPoeUpdateManager>> managerFactory)
    {
        uriChooser = new ResourceChooser<string>(uris);
        this.managerFactory = managerFactory;
    }

    public async Task<IPoeUpdateInfo> CheckForUpdate(
        bool ignoreDeltaUpdates,
        Action<int> progress = null)
    {
        return await SafeGet(x => x.CheckForUpdate(ignoreDeltaUpdates, progress));
    }

    public async Task<IReadOnlyCollection<FileInfo>> DownloadReleases(
        IReadOnlyCollection<IReleaseEntry> releasesToDownload,
        Action<int> progress = null)
    {
        return await SafeGet(x => x.DownloadReleases(releasesToDownload, progress));
    }

    public async Task<bool> VerifyReleases(IReadOnlyCollection<IReleaseEntry> releasesToDownload, Action<int> progress = null)
    {
        return await SafeGet(x => x.VerifyReleases(releasesToDownload, progress));
    }

    public async Task<string> ApplyReleases(
        IPoeUpdateInfo updateInfo,
        Action<int> progress = null)
    {
        return await SafeGet(x => x.ApplyReleases(updateInfo, progress));
    }

    public async Task<IPoeUpdateInfo> PrepareUpdate(
        bool ignoreDeltaUpdates,
        IReadOnlyCollection<IReleaseEntry> localReleases,
        IReadOnlyCollection<IReleaseEntry> remoteReleases)
    {
        return await SafeGet(
            x => x.PrepareUpdate(ignoreDeltaUpdates, localReleases, remoteReleases));
    }

    private async Task<T> SafeGet<T>(Expression<Func<IPoeUpdateManager, Task<T>>> getterExpr)
    {
        var log = Log.WithSuffix(getterExpr.ToString());
        
        log.Debug(() => $"Acquiring lock {gate}");
        using var @lock = gate.Enter();

        var allUris = uriChooser.ToArray();
        log.Debug(() => $"Contacting URIs in the following order: {allUris.DumpToString()}");
        foreach (var uri in allUris)
        {
            try
            {
                log.WithSuffix(uri).Debug(() => $"Contacting URIs: {uri}");
                var manager = await Task.Run(() => managerFactory(uri));
                var getter = getterExpr.Compile();
                
                log.WithSuffix(uri).Debug(() => $"Performing request");
                var result = await Task.Run(() => getter(manager));
                log.WithSuffix(uri).Debug(() => $"Got request result: {result.Dump()}");
                uriChooser.ReportAlive(uri);
                return result;
            }
            catch (Exception e)
            {
                log.WithSuffix(uri).Warn($"Failed to get result", e);
                uriChooser.ReportBroken(uri);
            }
        }

        throw new InvalidStateException($"Failed to perform request, URIs: {allUris.Dump()}");
    }
}