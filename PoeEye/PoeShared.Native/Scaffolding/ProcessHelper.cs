using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using PoeShared.Logging;

namespace PoeShared.Scaffolding;

public class ProcessHelper
{
    private static readonly IFluentLog Log = typeof(ProcessHelper).PrepareLogger();

    public static ProcessRunInfo RunCmdAs(string cmd, string arguments, bool showWindow = false)
    {
        Log.Info($"Preparing to execute application with admin permissions: {new {cmd, arguments}}");
        var startInfo = new ProcessStartInfo
        {
            FileName = cmd,
            Arguments = arguments,
            UseShellExecute = true,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = !showWindow,
            WindowStyle = showWindow ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden,
            ErrorDialog = true,
            Verb = "runas"
        };
        return RunCmd(startInfo);
    }
    
    public static ProcessRunInfo RunCmd(string cmd, string arguments, TimeSpan? timeout = null)
    {
        Log.Info($"Preparing to execute application: {new {cmd, arguments, timeout}}");

        var startInfo = new ProcessStartInfo
        {
            FileName = cmd,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            ErrorDialog = true,
        };
        return RunCmd(startInfo, timeout);
    }
    public static ProcessRunInfo RunCmd(ProcessStartInfo processStartInfo, TimeSpan? timeout = null)
    {
        Log.Info(@$"Preparing to execute application: {new
        {
            processStartInfo.FileName,
            processStartInfo.Arguments,
            processStartInfo.UseShellExecute,
            processStartInfo.Verb,
            processStartInfo.CreateNoWindow,
            processStartInfo.WindowStyle,
            processStartInfo.RedirectStandardInput,
            processStartInfo.RedirectStandardOutput,
            processStartInfo.RedirectStandardError,
            processStartInfo.LoadUserProfile,
            processStartInfo.ErrorDialog,
            timeout
        }}");
        using var process = new Process();
        process.StartInfo = processStartInfo;
        var stderr = new List<string>();
        var stdout = new List<string>();

        var processName = Path.GetFileName(processStartInfo.FileName);
        
        var log = Log.WithSuffix(processName);
        process.OutputDataReceived += (sender, eventArgs) =>
        {
            if (string.IsNullOrEmpty(eventArgs.Data))
            {
                return;
            }
            log.WithPrefix("STDOUT").Info(eventArgs.Data);
            stdout.Add(eventArgs.Data);
        };

        process.ErrorDataReceived += (sender, eventArgs) => {
            if (string.IsNullOrEmpty(eventArgs.Data))
            {
                return;
            }
            log.WithPrefix("STDERR").Warn($"{eventArgs.Data}");
            stderr.Add(eventArgs.Data);
        };
            
        Log.Info($"Starting process: {processStartInfo.FileName} {process.StartInfo.Arguments}");
        process.Start();
        try
        {
            var processIdInfo = $"Id {process.Id} @ {process.StartTime}";
            log = log.WithSuffix(processIdInfo);
        }
        catch (Exception e)
        {
            log.Warn($"Failed to get processId", e);
            log = log.WithSuffix($"Id Unknown");
        }
        
        if (process.StartInfo.RedirectStandardOutput)
        {
            log.Info($"Starting to read process output");
            process.BeginOutputReadLine();
            log.Info($"Started reading process output");
        }
        if (process.StartInfo.RedirectStandardError)
        {
            log.Info($"Starting to read process errors output");
            process.BeginErrorReadLine();
            log.Info($"Started reading process errors output");
        }
        
        log.Info($"Awaiting {timeout} for process to exit");
        var processExited = process.WaitForExit(timeout != null ? (int)timeout.Value.TotalMilliseconds : int.MaxValue);
        if (processExited == false)	
        {
            log.Warn("Process has failed to finish in time, killing it");
            process.Kill();
            log.Warn("Killed the process");
            throw new InvalidStateException("ERROR: Process took too long to finish");
        }
        if (process.HasExited)
        {
            log = log.WithSuffix($"ExitCode: {process.ExitCode} @ {process.ExitTime}");
        }
        log.Info("Application has exited");
        if (process.ExitCode < 0) 
        {
            Log.Warn("Process exited with non-zero exit code of: " + process.ExitCode);
        }
            
        if (stderr.Any()) 
        {
            throw new InvalidStateException("Process has written errors into output, exit code: " + process.ExitCode + Environment.NewLine + "ERRORS: " + Environment.NewLine + string.Join(Environment.NewLine, stderr));
        }
        return new ProcessRunInfo
        {
            ExitCode = process.ExitCode,
            StdErr = stderr,
            StdOut = stdout
        };
    }
    
    public readonly struct ProcessRunInfo
    {
        public int ExitCode { get; init; }
        
        public IReadOnlyList<string> StdErr { get; init; }
        public IReadOnlyList<string> StdOut { get; init; }

        public IReadOnlyList<string> Output => (StdOut ?? ArraySegment<string>.Empty).Concat(StdErr ?? ArraySegment<string>.Empty).ToList();
    }
}