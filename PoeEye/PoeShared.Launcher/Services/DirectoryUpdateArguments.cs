using CommandLine;

namespace PoeShared.Launcher.Services;

/// <summary>Private early-launcher transport for one captured plan and its application's ready/commit pipe.</summary>
internal sealed class DirectoryUpdateArguments
{
    [Option("directoryPlan", Required = true)]
    public string PlanPath { get; set; } = string.Empty;

    [Option("directoryPlanSha256", Required = true)]
    public string PlanSha256 { get; set; } = string.Empty;

    [Option("handoffPipe", Required = true)]
    public string PipeName { get; set; } = string.Empty;

    [Option("installPayload", Required = false)]
    public bool InstallPayload { get; set; }
}
