using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Text.Json;

namespace PoeShared.Cli;

public sealed class CliApplication<TAppOptions>
    where TAppOptions : class, new()
{
    private readonly string _appName;
    private readonly IReadOnlyList<ICliValueBinder<TAppOptions>> _globalBinders;
    private readonly IReadOnlyList<CliRecursiveOptionSpec> _recursiveOptions;
    private readonly Func<CliCommandResult, string?>? _humanFormatter;
    private readonly AsyncLocal<CliInvocationState?> _invocationState = new();
    private RootCommand? _rootCommand;

    internal CliApplication(
        string appName,
        string description,
        CliCommandBuilder<TAppOptions> rootBuilder,
        IReadOnlyList<ICliValueBinder<TAppOptions>> globalBinders,
        IReadOnlyList<CliRecursiveOptionSpec> recursiveOptions,
        Func<CliCommandResult, string?>? humanFormatter)
    {
        _appName = appName;
        Description = description;
        _globalBinders = globalBinders;
        _recursiveOptions =
        [
            new(["--json"], false),
            new(["--json-rpc"], false),
            new(["--interactive"], false),
            .. recursiveOptions
        ];
        _humanFormatter = humanFormatter;
        RootBuilder = rootBuilder;
    }

    public string Description { get; }

    public TextReader Input { get; private set; } = Console.In;

    public TextWriter Output { get; private set; } = Console.Out;

    public TextWriter Error { get; private set; } = Console.Error;

    internal CliCommandBuilder<TAppOptions> RootBuilder { get; }

    public CliApplication<TAppOptions> UseConsole(
        TextReader input,
        TextWriter output,
        TextWriter error)
    {
        Input = input;
        Output = output;
        Error = error;
        return this;
    }

    public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        var split = SplitRecursiveOptions(args);
        var inheritedGlobalArgs = split.GlobalArgs
            .Where(arg => !IsOptionName(arg, "--json-rpc") && !IsOptionName(arg, "--interactive"))
            .ToArray();

        if (split.JsonRpc)
        {
            var server = new CliJsonRpcServer<TAppOptions>(this, Input, Output, inheritedGlobalArgs);
            await server.RunAsync(cancellationToken);
            return 0;
        }

        if (split.Interactive)
        {
            return await RunInteractiveAsync(inheritedGlobalArgs, split.CommandArgs, cancellationToken);
        }

        return await InvokeAsync(args, captureOnly: false, cancellationToken);
    }

    internal async Task<CliCommandResult> InvokeForResultAsync(
        string[] args,
        CancellationToken cancellationToken)
    {
        var parseResult = RootCommand.Parse(args);
        if (parseResult.Errors.Count > 0)
        {
            return ParseFailure(parseResult);
        }

        var state = new CliInvocationState(captureOnly: true);
        _invocationState.Value = state;
        try
        {
            var exitCode = await parseResult.InvokeAsync(CreateInvocationConfiguration(), cancellationToken);
            return state.Result ??
                   CliCommandResult.Fail("parse", "invalidArguments", "Command did not produce a result.", exitCode);
        }
        finally
        {
            _invocationState.Value = null;
        }
    }

    internal async Task<int> ExecuteHandlerAsync(
        string commandName,
        ICliCommandHandler<TAppOptions> handler,
        IReadOnlyList<ICliValueBinder<TAppOptions>> globalBinders,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var commonOptions = new CliCommonOptions
        {
            Json = parseResult.GetValue(JsonOption),
            JsonRpc = parseResult.GetValue(JsonRpcOption),
            Interactive = parseResult.GetValue(InteractiveOption)
        };
        var appOptions = new TAppOptions();
        foreach (var binder in globalBinders)
        {
            binder.Bind(parseResult, appOptions);
        }

        var context = new CliExecutionContext<TAppOptions>(
            _appName,
            commonOptions,
            appOptions,
            Input,
            Output,
            Error,
            CliJson.Options,
            cancellationToken);

        CliCommandResult result;
        try
        {
            result = await handler.ExecuteAsync(parseResult, context);
        }
        catch (NotImplementedException exception)
        {
            result = CliCommandResult.NotImplemented(commandName, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            result = CliCommandResult.Fail(commandName, "clientError", exception.Message, 1);
        }

        var state = _invocationState.Value;
        if (state?.CaptureOnly == true)
        {
            state.Result = result;
            return result.ExitCode;
        }

        return await WriteResultAsync(result, commonOptions.Json);
    }

    private RootCommand RootCommand
    {
        get
        {
            if (_rootCommand is not null)
            {
                return _rootCommand;
            }

            var rootCommand = (RootCommand) RootBuilder.BuildCommand(this, _globalBinders, RootBuilder.Name);
            rootCommand.Options.Add(JsonOption);
            rootCommand.Options.Add(JsonRpcOption);
            rootCommand.Options.Add(InteractiveOption);
            foreach (var binder in _globalBinders)
            {
                if (binder.Symbol is Option option)
                {
                    rootCommand.Options.Add(option);
                }
            }

            _rootCommand = rootCommand;
            return rootCommand;
        }
    }

    private static Option<bool> JsonOption { get; } = new("--json")
    {
        Description = "Emit JSON for command results.",
        Recursive = true
    };

    private static Option<bool> JsonRpcOption { get; } = new("--json-rpc")
    {
        Description = "Start a JSON-RPC 2.0 stdio loop. Usually used alone.",
        Recursive = true
    };

    private static Option<bool> InteractiveOption { get; } = new("--interactive")
    {
        Description = "Read commands from stdin one line at a time.",
        Recursive = true
    };

    private async Task<int> RunInteractiveAsync(
        string[] inheritedGlobalArgs,
        string[] initialCommandArgs,
        CancellationToken cancellationToken)
    {
        var exitCode = 0;

        if (initialCommandArgs.Length > 0)
        {
            exitCode = await InvokeAsync([.. inheritedGlobalArgs, .. initialCommandArgs], false, cancellationToken);
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            if (!Console.IsInputRedirected)
            {
                await Error.WriteAsync($"{_appName}> ");
            }

            var line = await Input.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                line.Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (!CliCommandLineTokenizer.TrySplit(line, out var lineArgs, out var splitError))
            {
                exitCode = await WriteResultAsync(
                    CliCommandResult.Fail(
                        "parse",
                        "invalidCommandLine",
                        splitError ?? "Invalid command line.",
                        1),
                    inheritedGlobalArgs.Any(arg => IsOptionName(arg, "--json")));
                continue;
            }

            exitCode = await InvokeAsync([.. inheritedGlobalArgs, .. lineArgs], false, cancellationToken);
        }

        return exitCode;
    }

    private async Task<int> InvokeAsync(
        string[] args,
        bool captureOnly,
        CancellationToken cancellationToken)
    {
        var parseResult = RootCommand.Parse(args);
        if (parseResult.Errors.Count > 0 && args.Any(arg => IsOptionName(arg, "--json")))
        {
            return await WriteResultAsync(ParseFailure(parseResult), json: true);
        }

        var state = new CliInvocationState(captureOnly);
        _invocationState.Value = state;
        try
        {
            return await parseResult.InvokeAsync(CreateInvocationConfiguration(), cancellationToken);
        }
        finally
        {
            _invocationState.Value = null;
        }
    }

    private InvocationConfiguration CreateInvocationConfiguration()
    {
        return new InvocationConfiguration
        {
            Output = Output,
            Error = Error
        };
    }

    private async Task<int> WriteResultAsync(CliCommandResult result, bool json)
    {
        if (json)
        {
            await Output.WriteLineAsync(JsonSerializer.Serialize(result.ToEnvelope(), CliJson.Options));
            return result.ExitCode;
        }

        if (result.Error is not null)
        {
            await Error.WriteLineAsync(result.Error.Message);
        }

        if (result.Success)
        {
            await Output.WriteLineAsync(FormatHuman(result));
        }

        return result.ExitCode;
    }

    private string FormatHuman(CliCommandResult result)
    {
        var formatted = _humanFormatter?.Invoke(result);
        if (formatted is not null)
        {
            return formatted;
        }

        return result.Data switch
        {
            null => result.Command,
            _ => JsonSerializer.Serialize(result.Data, CliJson.Options)
        };
    }

    private CliCommandResult ParseFailure(ParseResult parseResult)
    {
        var errors = parseResult.Errors
            .Select(error => error.Message)
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .ToArray();
        return CliCommandResult.Fail(
            "parse",
            "invalidArguments",
            "Invalid command line arguments.",
            1,
            errors);
    }

    private SplitArgs SplitRecursiveOptions(string[] args)
    {
        var globalArgs = new List<string>();
        var commandArgs = new List<string>();
        var jsonRpc = false;
        var interactive = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            var spec = FindRecursiveOption(arg);
            if (spec is null)
            {
                commandArgs.Add(arg);
                continue;
            }

            globalArgs.Add(arg);
            jsonRpc |= IsOptionName(arg, "--json-rpc");
            interactive |= IsOptionName(arg, "--interactive");

            if (spec.TakesValue && !arg.Contains('=', StringComparison.Ordinal) && i + 1 < args.Length)
            {
                globalArgs.Add(args[++i]);
            }
        }

        return new SplitArgs(globalArgs.ToArray(), commandArgs.ToArray(), jsonRpc, interactive);
    }

    private CliRecursiveOptionSpec? FindRecursiveOption(string arg)
    {
        var optionName = GetOptionName(arg);
        if (optionName is null)
        {
            return null;
        }

        return _recursiveOptions.FirstOrDefault(spec =>
            spec.Aliases.Any(alias => string.Equals(alias, optionName, StringComparison.OrdinalIgnoreCase)));
    }

    private static string? GetOptionName(string arg)
    {
        if (!arg.StartsWith("--", StringComparison.Ordinal))
        {
            return null;
        }

        var equalsIndex = arg.IndexOf('=');
        return equalsIndex < 0 ? arg : arg[..equalsIndex];
    }

    private static bool IsOptionName(string arg, string expected)
    {
        return string.Equals(GetOptionName(arg), expected, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record SplitArgs(
        string[] GlobalArgs,
        string[] CommandArgs,
        bool JsonRpc,
        bool Interactive);

    private sealed class CliInvocationState
    {
        public CliInvocationState(bool captureOnly)
        {
            CaptureOnly = captureOnly;
        }

        public bool CaptureOnly { get; }

        public CliCommandResult? Result { get; set; }
    }
}
