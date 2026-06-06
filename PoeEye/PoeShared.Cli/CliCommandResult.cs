using System.Text.Json.Serialization;

namespace PoeShared.Cli;

public sealed record CliCommandResult(
    bool Success,
    string Command,
    object? Data,
    CliCommandError? Error,
    int ExitCode)
{
    public static CliCommandResult Ok(string command, object? data)
    {
        return new CliCommandResult(true, command, data, null, 0);
    }

    public static CliCommandResult Fail(
        string command,
        string code,
        string message,
        int exitCode,
        object? details = null)
    {
        return new CliCommandResult(false, command, null, new CliCommandError(code, message, details), exitCode);
    }

    public static CliCommandResult NotImplemented(string command, string message)
    {
        return Fail(command, "notImplemented", message, 2);
    }

    public CliCommandEnvelope ToEnvelope()
    {
        return new CliCommandEnvelope(Success, Command, Data, Error);
    }
}

public sealed record CliCommandEnvelope(
    [property: JsonPropertyName("ok")] bool Ok,
    [property: JsonPropertyName("command")] string Command,
    [property: JsonPropertyName("data")] object? Data,
    [property: JsonPropertyName("error")] CliCommandError? Error);

public sealed record CliCommandError(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("details")] object? Details = null);
