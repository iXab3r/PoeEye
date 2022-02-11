namespace PoeShared.GCLog;

internal sealed class ClrTypesInfo
{
    private readonly Dictionary<ulong, string> typesIdToName;

    public ClrTypesInfo()
    {
        typesIdToName = new Dictionary<ulong, string>();
    }

    public string this[ulong id]
    {
        get
        {
            if (!typesIdToName.ContainsKey(id))
            {
                return null;
            }

            return typesIdToName[id];
        }
        set => typesIdToName[id] = value;
    }
}