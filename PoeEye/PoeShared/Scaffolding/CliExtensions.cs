using System.Runtime.CompilerServices;
using System.Text;
using CliWrap;
using CliWrap.EventStream;

namespace PoeShared.Scaffolding;

public static class CliExtensions
{
    private static readonly IFluentLog Log = typeof(CliExtensions).PrepareLogger();

    public static async IAsyncEnumerable<CommandEvent> ListenAndLogAsync(this Command command, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var anchors = new CompositeDisposable();
        int? processId = null;
        var log = Log.WithSuffix(() => $"PID: {(processId == null ? "not started" : processId.Value)}");
        try
        {
            log.Info($"Running command: {command}");

            await foreach (var cmdEvent in ListenAsync(log, command, Encoding.Default, Encoding.Default, cancellationToken))
            {
                switch (cmdEvent)
                {
                    case StartedCommandEvent started:
                        processId = started.ProcessId;
                        log.AddPrefix($"PID {started.ProcessId}");
                        log.Info($"Process started; ID: {started.ProcessId}");
                        
                        cancellationToken.Register(() =>
                        {
                            log.Warn($"Forcefully cancelling command process(id {started.ProcessId}): {command}");
                            TerminateProcessById(started.ProcessId, log);
                            log.Warn($"Forcefully terminated process(id {started.ProcessId}): {command}");
                        }).AddTo(anchors);
                        
                        break;
                    case StandardOutputCommandEvent stdOut:
                        log.Debug($"Out> {stdOut.Text}");
                        break;
                    case StandardErrorCommandEvent stdErr:
                        log.Warn($"Err> {stdErr.Text}");
                        break;
                    case ExitedCommandEvent exited:
                        log.Info($"Process exited; Code: {exited.ExitCode}");
                        break;
                }

                yield return cmdEvent;
            }
        }
        finally
        {
            log.Info("Command completed");
            TerminateProcessById(processId, log);
        }
    }

    /// <summary>
    /// There is a problem with CliWrap ListenAsync - it does not correctly process cancellation and throws unobserved exception upon GC
    /// It happens because SemaphoreSlim used by Channel is already disposed(due to cancellation) when the process completes
    /// </summary>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async IAsyncEnumerable<CommandEvent> ListenAndLogAsyncErroneous(this Command command, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var log = Log.WithSuffix(command.ToString().TakeMidChars(64));
        int? processId = null;
        try
        {
            log.Debug($"Running command: {command}");

            await foreach (var cmdEvent in command.ListenAsync(cancellationToken))
            {
                switch (cmdEvent)
                {
                    case StartedCommandEvent started:
                        processId = started.ProcessId;
                        log.AddPrefix($"PID {started.ProcessId}");
                        log.Debug($"Process started; ID: {started.ProcessId}");
                        break;
                    case StandardOutputCommandEvent stdOut:
                        log.Debug($"Out> {stdOut.Text}");
                        break;
                    case StandardErrorCommandEvent stdErr:
                        log.Debug($"Err> {stdErr.Text}");
                        break;
                    case ExitedCommandEvent exited:
                        log.Debug($"Process exited; Code: {exited.ExitCode}");
                        break;
                }

                yield return cmdEvent;
            }
        }
        finally
        {
            log.Debug("Command completed");
            TerminateProcessById(processId, log);
        }
    }

    private static void TerminateProcessById(int? processId, IFluentLog log)
    {
        if (processId == null)
        {
            log.Warn($"Process has not even started");
            return;
        }

        Process process;
        try
        {
            process = Process.GetProcessById(processId.Value);
        }
        catch (ArgumentException)
        {
            // usually this means that the process has already terminated
            log.Debug($"Tried to terminate the process - could not find it by Id {processId.Value}, probably it has already terminated itself");
            return;
        }

        if (process.HasExited)
        {
            log.Debug($"Process has terminated itself: {new {process.ExitTime, process.ExitCode}}");
            return;
        }

        try
        {
            log.Warn("Process has not terminated, killing it");
            process.Kill();
            log.Warn("Killed the process and its childs");
        }
        catch (Exception e)
        {
            log.Warn($"Failed to kill the process {processId}", e);
        }
    }

    private static async IAsyncEnumerable<CommandEvent> ListenAsync(
        IFluentLog log,
        Command command,
        Encoding standardOutputEncoding,
        Encoding standardErrorEncoding,
        [EnumeratorCancellation] CancellationToken forcefulCancellationToken)
    {
        foreach (var commandEvent in ListenAsync(log, command, standardOutputEncoding, standardErrorEncoding, forcefulCancellationToken, CancellationToken.None))
        {
            yield return commandEvent;
        }
    }

    private static IObservable<CommandEvent> ListenAsync(
        IFluentLog log,
         Command command,
        Encoding standardOutputEncoding,
        Encoding standardErrorEncoding,
        CancellationToken forcefulCancellationToken,
        CancellationToken gracefulCancellationToken)
    {
        return Observable.Create<CommandEvent>(async observer =>
        {
            var anchors = new CompositeDisposable();
            var stdOutPipe = PipeTarget.Merge(
                command.StandardOutputPipe,
                PipeTarget.ToDelegate(async (line, innerCancellationToken) =>
                {
                    observer.OnNext(new StandardOutputCommandEvent(line));
                }, standardOutputEncoding)
            );

            var stdErrPipe = PipeTarget.Merge(
                command.StandardErrorPipe,
                PipeTarget.ToDelegate(async (line, innerCancellationToken) =>
                {
                    observer.OnNext(new StandardErrorCommandEvent(line));
                }, standardErrorEncoding)
            );

            var commandWithPipes = command
                .WithStandardOutputPipe(stdOutPipe)
                .WithStandardErrorPipe(stdErrPipe);

            try
            {
                log.Debug("Starting the process...");
                var commandTask = commandWithPipes.ExecuteAsync(forcefulCancellationToken, gracefulCancellationToken);
                log.Debug($"Process started, processId: {commandTask.ProcessId}");
                observer.OnNext(new StartedCommandEvent(commandTask.ProcessId));
                log.Debug($"Awaiting for process completion");
                var result = await commandTask.ConfigureAwait(false);
                log.Debug($"Process {commandTask.ProcessId} has exited: { new { result.ExitCode, result.ExitTime, result.RunTime, result.StartTime } }");
                observer.OnNext(new ExitedCommandEvent(result.ExitCode));
                observer.OnCompleted();
            }
            catch (Exception e)
            {
                observer.OnError(e);
            }

            return anchors;

        });
    }
}