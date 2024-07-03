using CommandLine;

namespace PoeShared.Launcher.Services;

internal record StartAppArguments
{
    [Option("exePath", Required = false, HelpText = "Path to executable")] 
    public string ExecutablePath { get; set; }

    [Option("exeArguments", Required = false, HelpText = "Executable arguments")] 
    public string Arguments { get; set; }
    
    [Option("exeVerb", Required = false, HelpText = "Executable verb")] 
    public string Verb { get; set; }
}

internal sealed record RestartAppArguments : StartAppArguments
{
    [Option("processId", Required = false, HelpText = "ProcessId that must be terminated before starting the app")] 
    public int? ProcessIdToWait { get; set; }
    
    [Option("timeoutMs", Required = false, HelpText = "Timeout")] 
    public long TimeoutMs { get; set; }
}

internal sealed record SwapAppArguments : StartAppArguments
{
    [Option("processId", Required = true, HelpText = "ProcessId that must be terminated before starting the app")] 
    public int ProcessIdToWait { get; set; }
    
    [Option("timeoutMs", Required = true, HelpText = "Timeout")] 
    public long TimeoutMs { get; set; }
}