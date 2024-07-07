using CommandLine;

namespace PoeShared.Launcher;

internal sealed record LauncherArguments 
{
    [Option("launcherMethod", Required = true, HelpText = "Launcher Method name")]
    public string Method { get; set; }
}