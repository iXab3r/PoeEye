using System.Text.Json;

namespace PoeShared.Cli;

public class CliExecutionContext
{
    public CliExecutionContext(
        string appName,
        CliCommonOptions commonOptions,
        TextReader input,
        TextWriter output,
        TextWriter error,
        JsonSerializerOptions jsonOptions,
        CancellationToken cancellationToken)
    {
        AppName = appName;
        CommonOptions = commonOptions;
        Input = input;
        Output = output;
        Error = error;
        JsonOptions = jsonOptions;
        CancellationToken = cancellationToken;
    }

    public string AppName { get; }

    public CliCommonOptions CommonOptions { get; }

    public TextReader Input { get; }

    public TextWriter Output { get; }

    public TextWriter Error { get; }

    public JsonSerializerOptions JsonOptions { get; }

    public CancellationToken CancellationToken { get; }
}

public sealed class CliExecutionContext<TAppOptions> : CliExecutionContext
    where TAppOptions : class
{
    public CliExecutionContext(
        string appName,
        CliCommonOptions commonOptions,
        TAppOptions appOptions,
        TextReader input,
        TextWriter output,
        TextWriter error,
        JsonSerializerOptions jsonOptions,
        CancellationToken cancellationToken)
        : base(appName, commonOptions, input, output, error, jsonOptions, cancellationToken)
    {
        AppOptions = appOptions;
    }

    public TAppOptions AppOptions { get; }
}
