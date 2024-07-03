using CommandLine;

namespace PoeShared.Launcher;

internal sealed record LauncherArguments 
{
    [Option("method", Required = true, HelpText = "Launcher Method name")]
    public string Method { get; set; }
    
    [Option("silent", HelpText = "If silent, no window will be shown")]
    public bool IsSilent { get; set; }
}