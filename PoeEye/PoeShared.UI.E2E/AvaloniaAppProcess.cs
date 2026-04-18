using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;

namespace PoeShared.UI.E2E;

internal sealed class AvaloniaAppProcess : IAsyncDisposable
{
    private const string ContentModeArgumentName = "--content-mode";
    private const string SampleViewArgumentName = "--sample-view";
    private readonly Process process;
    private readonly int debugPort;

    private AvaloniaAppProcess(Process process, int debugPort)
    {
        this.process = process;
        this.debugPort = debugPort;
    }

    public int DebugPort => debugPort;

    public int ProcessId => process.Id;

    public static AvaloniaAppProcess Start(int debugPort, params string[] extraArguments)
        => StartCore(debugPort, contentMode: null, sampleView: null, extraArguments);

    public static AvaloniaAppProcess Start(int debugPort, string? contentMode, params string[] extraArguments)
        => StartCore(debugPort, contentMode, sampleView: null, extraArguments);

    public static AvaloniaAppProcess StartBlazor(int debugPort, params string[] extraArguments)
        => Start(debugPort, AvaloniaContentMode.Blazor, extraArguments);

    public static AvaloniaAppProcess StartBlazor(int debugPort, AvaloniaSampleView sampleView, params string[] extraArguments)
        => StartCore(debugPort, AvaloniaContentMode.Blazor, sampleView.ToCommandLineKey(), extraArguments);

    public static AvaloniaAppProcess StartWindowHarness(int debugPort, params string[] extraArguments)
        => Start(debugPort, AvaloniaContentMode.Blazor, extraArguments);

    public async Task WaitForBrowserReadyAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var httpClient = new HttpClient();
        var versionUrl = $"http://127.0.0.1:{debugPort}/json/version";
        var deadline = DateTimeOffset.UtcNow + timeout;

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (process.HasExited)
            {
                var stderr = await process.StandardError.ReadToEndAsync();
                var stdout = await process.StandardOutput.ReadToEndAsync();
                throw new InvalidOperationException($"PoeShared.UI.Avalonia exited early with code {process.ExitCode}. StdOut: {stdout} StdErr: {stderr}");
            }

            try
            {
                var response = await httpClient.GetAsync(versionUrl, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
                // retry until timeout
            }

            await Task.Delay(250, cancellationToken);
        }

        throw new TimeoutException($"Timed out waiting for WebView2 CDP endpoint at {versionUrl}");
    }

    public async Task<IntPtr> WaitForMainWindowHandleAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (process.HasExited)
            {
                throw new InvalidOperationException($"PoeShared.UI.Avalonia exited early with code {process.ExitCode}.");
            }

            process.Refresh();
            if (process.MainWindowHandle != IntPtr.Zero)
            {
                return process.MainWindowHandle;
            }

            await Task.Delay(200, cancellationToken);
        }

        throw new TimeoutException("Timed out waiting for Avalonia main window handle.");
    }

    public async Task<IReadOnlyList<CdpTargetInfo>> ListBrowserTargetsAsync(CancellationToken cancellationToken = default)
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetStringAsync($"http://127.0.0.1:{debugPort}/json/list", cancellationToken);
        using var document = JsonDocument.Parse(response);
        return document.RootElement
            .EnumerateArray()
            .Select(x => new CdpTargetInfo(
                x.TryGetProperty("id", out var id) ? id.GetString() ?? string.Empty : string.Empty,
                x.TryGetProperty("type", out var type) ? type.GetString() ?? string.Empty : string.Empty,
                x.TryGetProperty("title", out var title) ? title.GetString() ?? string.Empty : string.Empty,
                x.TryGetProperty("url", out var url) ? url.GetString() ?? string.Empty : string.Empty,
                x.TryGetProperty("devtoolsFrontendUrl", out var devToolsFrontendUrl) ? devToolsFrontendUrl.GetString() ?? string.Empty : string.Empty))
            .ToArray();
    }

    public async ValueTask DisposeAsync()
    {
        if (!process.HasExited)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // ignored
            }
        }

        process.Dispose();
        await Task.CompletedTask;
    }

    private static string ResolveRepoRoot()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "PoeEye"));
    }

    private static string ResolveProjectPath(string projectFolderName, string projectFileName)
    {
        return Path.Combine(ResolveRepoRoot(), projectFolderName, projectFileName);
    }

    private static Process StartProcess(
        string toolName,
        string workingDirectory,
        string projectPath,
        int debugPort,
        string? contentMode,
        string? sampleView,
        IReadOnlyList<string> extraArguments,
        bool useNoBuild)
    {
        var startInfo = new ProcessStartInfo(toolName)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectPath);
        if (useNoBuild)
        {
            startInfo.ArgumentList.Add("--no-build");
        }

        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add($"--webview2-debug-port={debugPort}");
        if (!string.IsNullOrWhiteSpace(contentMode))
        {
            startInfo.ArgumentList.Add(ContentModeArgumentName);
            startInfo.ArgumentList.Add(contentMode);
        }

        if (!string.IsNullOrWhiteSpace(sampleView))
        {
            startInfo.ArgumentList.Add(SampleViewArgumentName);
            startInfo.ArgumentList.Add(sampleView);
        }

        foreach (var extraArgument in extraArguments)
        {
            startInfo.ArgumentList.Add(extraArgument);
        }

        return Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start PoeShared.UI.Avalonia");
    }

    private static Process StartBuiltExecutable(
        string executablePath,
        string workingDirectory,
        int debugPort,
        string? contentMode,
        string? sampleView,
        IReadOnlyList<string> extraArguments)
    {
        var startInfo = new ProcessStartInfo(executablePath)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add($"--webview2-debug-port={debugPort}");
        if (!string.IsNullOrWhiteSpace(contentMode))
        {
            startInfo.ArgumentList.Add(ContentModeArgumentName);
            startInfo.ArgumentList.Add(contentMode);
        }

        if (!string.IsNullOrWhiteSpace(sampleView))
        {
            startInfo.ArgumentList.Add(SampleViewArgumentName);
            startInfo.ArgumentList.Add(sampleView);
        }

        foreach (var extraArgument in extraArguments)
        {
            startInfo.ArgumentList.Add(extraArgument);
        }

        return Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start PoeShared.UI.Avalonia executable");
    }

    private static AvaloniaAppProcess StartCore(int debugPort, string? contentMode, string? sampleView, params string[] extraArguments)
    {
        var projectPath = ResolveProjectPath("PoeShared.UI.Avalonia", "PoeShared.UI.Avalonia.csproj");
        var workingDirectory = ResolveRepoRoot();
        var extraArgumentList = extraArguments?.Where(argument => !string.IsNullOrWhiteSpace(argument)).ToArray() ?? Array.Empty<string>();
        var builtExePath = Path.Combine(
            Path.GetDirectoryName(projectPath)!,
            "bin",
            "Debug",
            "net8.0-windows10.0.19041.0",
            "PoeShared.UI.Avalonia.exe");

        if (File.Exists(builtExePath))
        {
            return new AvaloniaAppProcess(
                StartBuiltExecutable(builtExePath, workingDirectory, debugPort, contentMode, sampleView, extraArgumentList),
                debugPort);
        }

        return new AvaloniaAppProcess(
            StartProcess("dotnet", workingDirectory, projectPath, debugPort, contentMode, sampleView, extraArgumentList, useNoBuild: false),
            debugPort);
    }

    internal sealed record CdpTargetInfo(
        string Id,
        string Type,
        string Title,
        string Url,
        string DevToolsFrontendUrl);
}
