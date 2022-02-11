using System.Collections.Concurrent;

namespace PoeShared.GCLog;

internal sealed class ContentionInfoStore
{
    private readonly ConcurrentDictionary<int, ProcessContentionInfo> perProcessContentionInfo = new();

    public void AddProcess(int processId)
    {
        var info = new ProcessContentionInfo(processId);
        perProcessContentionInfo.TryAdd(processId, info);
    }

    public void RemoveProcess(int processId)
    {
        ProcessContentionInfo info;
        perProcessContentionInfo.TryRemove(processId, out info);
    }

    public ContentionInfo GetContentionInfo(int processId, int threadId)
    {
        ProcessContentionInfo processInfo;
        if (perProcessContentionInfo.TryGetValue(processId, out processInfo))
        {
            return processInfo.GetContentionInfo(threadId);
        }

        return null;
    }
}