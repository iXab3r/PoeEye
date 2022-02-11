namespace PoeShared.GCLog;

internal readonly struct ProcessContentionInfo
{
    private readonly int processId;
    private readonly Dictionary<int, ContentionInfo> perThreadContentionInfo;

    public ProcessContentionInfo(int processId)
    {
        this.processId = processId;
        perThreadContentionInfo = new Dictionary<int, ContentionInfo>();
    }

    public ContentionInfo GetContentionInfo(int threadId)
    {
        ContentionInfo contentionInfo;
        if (!perThreadContentionInfo.TryGetValue(threadId, out contentionInfo))
        {
            contentionInfo = new ContentionInfo(processId, threadId);
            perThreadContentionInfo.Add(threadId, contentionInfo);
        }

        return contentionInfo;
    }
}