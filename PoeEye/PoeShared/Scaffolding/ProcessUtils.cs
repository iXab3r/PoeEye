namespace PoeShared.Scaffolding;

public static class ProcessUtils
{
    private static readonly IFluentLog Log = typeof(ProcessUtils).PrepareLogger();
    private static readonly string ExplorerExecutablePath = Environment.ExpandEnvironmentVariables(@"%WINDIR%\explorer.exe");

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

    public static async Task SelectFileOrFolder(FileSystemInfo fileSystemInfo)
    {
        await Task.Run(
            () =>
            {
                Log.Debug(() => $"Selecting: {fileSystemInfo}");
                fileSystemInfo.Refresh();
                if (!fileSystemInfo.Exists)
                {
                    throw new InvalidOperationException($"{fileSystemInfo} does not exist");
                }

                Process.Start(ExplorerExecutablePath, $"/select,\"{fileSystemInfo.FullName}\"");
            });
    }
    
    public static async Task OpenFolder(DirectoryInfo directory)
    {
        await Task.Run(
            () =>
            {
                var appDirectory = directory.FullName;
                Log.Debug(() => $"Opening App directory: {appDirectory}");
                if (!directory.Exists)
                {
                    throw new InvalidOperationException($"Directory {appDirectory} does not exist");
                }

                Process.Start(ExplorerExecutablePath, $"\"{appDirectory}\"");
            });
    }
}