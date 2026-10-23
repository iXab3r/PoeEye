#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PoeShared.Launcher;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Services;
using Unity;

namespace PoeShared.Squirrel.Updater;

/// <summary>
/// Connects a fixed local installation to the product's acknowledged launcher helper.
/// Its lifetime belongs to the application, independently of the supplying author scope.
/// </summary>
internal sealed class ApplicationUpdateInstallationService
{
    private readonly IAppArguments app;
    private readonly IApplicationAccessor application;
    private readonly IApplicationUpdaterInstallationInfoProvider installationInfo;
    private readonly IScheduler uiScheduler;

    public ApplicationUpdateInstallationService(IAppArguments app, IApplicationAccessor application,
        IApplicationUpdaterInstallationInfoProvider installationInfo,
        [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
    {
        this.app = app;
        this.application = application;
        this.installationInfo = installationInfo;
        this.uiScheduler = uiScheduler;
    }

    internal async Task ApplyAsync(string payloadDirectory, string executableName, bool installPayload,
        IProgressReporter progress, CancellationToken cancellationToken,
        string? destinationDirectory = null, Func<Task>? finalizeInstallation = null)
    {
        if (progress is not IApplicationUpdateAcceptance acceptance)
            throw new InvalidOperationException("Installation requires the current application-owned Apply operation.");
        cancellationToken.ThrowIfCancellationRequested();
        using var parent = Process.GetCurrentProcess();
        // The product's installation provider resolves a phantom clone back to its real launcher.
        var runningExecutable = installationInfo.LauncherExecutable.FullName;
        var installationDirectory = destinationDirectory ?? (installPayload ? Path.GetDirectoryName(runningExecutable)! : payloadDirectory);
        var arguments = CaptureRestartArguments(app);
        var parentId = parent.Id;
        var parentStartedUtc = parent.StartTime.ToUniversalTime();
        var plan = await Task.Run(() => DirectoryUpdatePlan.Create(payloadDirectory, installationDirectory,
            Path.GetFileName(runningExecutable), executableName, arguments,
            parentId, parentStartedUtc), cancellationToken).ConfigureAwait(false);
        var handoffDirectory = Path.Combine(app.AppDataDirectory, ".updater", "handoffs", Guid.NewGuid().ToString("N"));
        using var handoff = await DirectoryUpdateHandoff.StartAsync(Path.Combine(payloadDirectory, executableName),
            plan, handoffDirectory, installPayload, cancellationToken).ConfigureAwait(false);
        // Acceptance and its synchronous public notification run on the application dispatcher.
        // Native finalization belongs to the application after Reserve, with final Commit only
        // on success. A reserved helper aborts on EOF and never launches a partially finalized B.
        var accepted = false;
        try
        {
            await uiScheduler.RunAsync(() =>
            {
                acceptance.Accept(() =>
                {
                    if (finalizeInstallation == null) handoff.Commit();
                    else handoff.ReserveInstallation();
                    accepted = true;
                });
                return true;
            }).ConfigureAwait(false);
        }
        finally
        {
            // Complete even if a synchronous HandedOff observer throws after acceptance.
            if (accepted)
            {
                if (finalizeInstallation != null)
                {
                    await finalizeInstallation().ConfigureAwait(false);
                    handoff.Commit();
                }
                await uiScheduler.RunAsync(() => { application.Exit(); return true; }).ConfigureAwait(false);
            }
        }
    }

    /// <summary>Captures original process tokens without reusing StartupArgs' unquoted space-joined representation.</summary>
    internal static string CaptureRestartArguments(IAppArguments app)
    {
        var original = app.CommandLineArguments ?? Array.Empty<string>();
        var separator = Array.IndexOf(original, "--");
        var arguments = string.Join(" ", original
            .Where((argument, index) => separator >= 0 && index > separator ||
                !string.Equals(argument, app.AutostartFlag, StringComparison.OrdinalIgnoreCase))
            .Select(QuoteArgument));
        // Retain the existing transport used by protected application launchers.
        return arguments.Contains('-') ? StringUtils.ToHexGzip(arguments) : arguments;
    }

    private static string QuoteArgument(string argument)
    {
        var quoted = new StringBuilder("\"");
        var backslashes = 0;
        foreach (var character in argument)
        {
            if (character == '\\')
            {
                backslashes++;
                continue;
            }
            quoted.Append('\\', character == '"' ? backslashes * 2 + 1 : backslashes);
            quoted.Append(character);
            backslashes = 0;
        }
        // Backslashes preceding a closing quote must be escaped just like those preceding a literal quote.
        return quoted.Append('\\', backslashes * 2).Append('"').ToString();
    }
}

/// <summary>
/// An operation-bound capability carried only by the application's real Apply reporter.
/// Public author Task completion cannot manufacture installer acceptance.
/// </summary>
internal interface IApplicationUpdateAcceptance
{
    void Accept(Action commitHelper);
}
