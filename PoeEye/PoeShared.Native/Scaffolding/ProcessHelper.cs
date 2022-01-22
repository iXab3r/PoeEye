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

    public static void RunCmd(ProcessStartInfo processStartInfo, TimeSpan? timeout = null)
    {
        Log.Info($"Preparing to execute application: {new {cmdPath = processStartInfo.FileName, timeout}}");
        using var process = new Process();
        process.StartInfo = processStartInfo;
        var stderr = new List<string>();
            
        IFluentLog GetLogger()
        {
            var processName = Path.GetFileName(processStartInfo.FileName);
            var result = Log.WithSuffix(processName);
            if (process.StartTime >= DateTime.MinValue)
            {
                result = result.WithSuffix($"Id {process.Id} @ {process.StartTime}");
            }
            if (process.HasExited)
            {
                result = result.WithSuffix($"ExitCode: {process.ExitCode} @ {process.ExitTime}");
            }
            return result;
        }

        if (processStartInfo.RedirectStandardOutput)
        {
            process.OutputDataReceived += (sender, eventArgs) =>
            {
                if (string.IsNullOrEmpty(eventArgs.Data))
                {
                    return;
                }
                GetLogger().WithPrefix("STDOUT").Info($"STDOUT: {eventArgs.Data}");
            };
        }

        if (processStartInfo.RedirectStandardError)
        {
            process.ErrorDataReceived += (sender, eventArgs) => {
                if (string.IsNullOrEmpty(eventArgs.Data))
                {
                    return;
                }
                GetLogger().WithPrefix("STDERR").Warn($"{eventArgs.Data}");
                stderr.Add(eventArgs.Data);
            };
        }
            
        Log.Info($"Starting process: {processStartInfo.FileName} {process.StartInfo.Arguments}");
        process.Start();

        if (process.StartInfo.RedirectStandardOutput)
        {
            process.BeginOutputReadLine();
        }

        if (process.StartInfo.RedirectStandardError)
        {
            process.BeginErrorReadLine();
        }
            
        GetLogger().Info($"Awaiting {timeout} for process to exit");
        var processExited = process.WaitForExit(timeout != null ? (int)timeout.Value.TotalMilliseconds : int.MaxValue);
        if (processExited == false)	
        {
            GetLogger().Warn("Process has failed to finish in time, killing it");
            process.Kill();
            GetLogger().Warn("Killed the process");
            throw new Exception("ERROR: Process took too long to finish");
        }
            
        GetLogger().Info("Application has exited");
        if (process.ExitCode != 0) 
        {
            throw new Exception("Process exited with non-zero exit code of: " + process.ExitCode);
        } 
            
        if (stderr.Any()) 
        {
            throw new Exception("Process has written errors into output, exit code: " + process.ExitCode + Environment.NewLine + "ERRORS: " + Environment.NewLine + string.Join(Environment.NewLine, stderr));
        }
    }
}