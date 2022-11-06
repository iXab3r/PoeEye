namespace PoeShared.Scaffolding;

public static class ProcessUtils
{
    private static readonly IFluentLog Log = typeof(ProcessUtils).PrepareLogger();

    public static async Task OpenUri(string uri)
    {
        Log.Debug(() => $"Preparing to open uri {uri}");
        await Task.Run(() =>
        {
            Log.Debug(() => $"Starting new process for uri: {uri}");
            var result = new Process {StartInfo = {FileName = uri, UseShellExecute = true}};
            if (!result.Start())
            {
                Log.Warn($"Failed to start process");
            }
            else
            {
                Log.Debug(() => $"Started new process for uri {uri}: { new { result.Id, result.ProcessName } }");
            }
        });
    }
}