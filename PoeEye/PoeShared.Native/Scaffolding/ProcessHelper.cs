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

    public static void RunCmd(string cmd, string arguments, TimeSpan? timeout = null)
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
        RunCmd(startInfo, timeout);
    }
    public static void RunCmd(ProcessStartInfo processStartInfo, TimeSpan? timeout = null)
    {
        Log.Info($"Preparing to execute application: {new {cmdPath = processStartInfo.FileName, timeout}}");
        using var process = new Process();
        process.StartInfo = processStartInfo;
        var stderr = new List<string>();

        var processName = Path.GetFileName(processStartInfo.FileName);
        
        var log = Log.WithSuffix(processName);
        if (processStartInfo.RedirectStandardOutput)
        {
            process.OutputDataReceived += (sender, eventArgs) =>
            {
                if (string.IsNullOrEmpty(eventArgs.Data))
                {
                    return;
                }
                log.WithPrefix("STDOUT").Info(eventArgs.Data);
            };
        }

        if (processStartInfo.RedirectStandardError)
        {
            process.ErrorDataReceived += (sender, eventArgs) => {
                if (string.IsNullOrEmpty(eventArgs.Data))
                {
                    return;
                }
                log.WithPrefix("STDERR").Warn($"{eventArgs.Data}");
                stderr.Add(eventArgs.Data);
            };
        }
            
        Log.Info($"Starting process: {processStartInfo.FileName} {process.StartInfo.Arguments}");
        process.Start();
        try
        {
            log = log.WithSuffix($"Id {process.Id} @ {process.StartTime}");
            
            log.Info(() => $"Starting to read process output");
            if (process.StartInfo.RedirectStandardOutput)
            {
                process.BeginOutputReadLine();
            }
            if (process.StartInfo.RedirectStandardError)
            {
                process.BeginErrorReadLine();
            }
            log.Info(() => $"Started reading process output");
        }
        catch (Exception e)
        {
            log.Warn($"Failed to get processId", e);
            log = log.WithSuffix($"Id Unknown");
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
        log.Info(() => $"Application has successfully completed");
    }
}