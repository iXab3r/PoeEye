using System.CommandLine;
using System.CommandLine.Parsing;

namespace PoeShared.Cli;

public sealed class CliApplicationBuilder<TAppOptions>
    where TAppOptions : class, new()
{
    private readonly List<ICliValueBinder<TAppOptions>> _globalBinders = [];
    private readonly List<CliRecursiveOptionSpec> _recursiveOptions = [];

    public CliApplicationBuilder(string appName, string description)
    {
        AppName = appName;
        Description = description;
        Root = new CliCommandBuilder<TAppOptions>(appName, description);
    }

    public string AppName { get; }

    public string Description { get; }

    public CliCommandBuilder<TAppOptions> Root { get; }

    public CliApplicationBuilder<TAppOptions> AddGlobalOption<TValue>(
        string name,
        string description,
        Action<TAppOptions, TValue?> assign,
        params string[] aliases)
    {
        var allAliases = BuildAliases(name, aliases);
        var option = new Option<TValue>(allAliases[0], allAliases.Skip(1).ToArray())
        {
            Description = description,
            Recursive = true
        };

        _globalBinders.Add(new CliValueBinder<TAppOptions, TValue>(option, assign));
        _recursiveOptions.Add(new CliRecursiveOptionSpec(allAliases, typeof(TValue) != typeof(bool)));
        return this;
    }

    public CliApplication<TAppOptions> Build(Func<CliCommandResult, string?>? humanFormatter = null)
    {
        return new CliApplication<TAppOptions>(
            AppName,
            Description,
            Root,
            _globalBinders,
            _recursiveOptions,
            humanFormatter);
    }

    internal static string[] BuildAliases(string name, string[] aliases)
    {
        return aliases.Length == 0 ? [name] : [name, .. aliases];
    }
}

public sealed class CliCommandBuilder<TAppOptions>
    where TAppOptions : class, new()
{
    private readonly List<CliCommandBuilder<TAppOptions>> _children = [];
    private ICliCommandHandler<TAppOptions>? _handler;

    internal CliCommandBuilder(string name, string description)
    {
        Name = name;
        Description = description;
    }

    public string Name { get; }

    public string Description { get; }

    internal IReadOnlyList<CliCommandBuilder<TAppOptions>> Children => _children;

    internal ICliCommandHandler<TAppOptions>? Handler => _handler;

    public CliCommandBuilder<TAppOptions> AddCommand(
        string name,
        string description,
        Action<CliCommandBuilder<TAppOptions>> configure)
    {
        var child = new CliCommandBuilder<TAppOptions>(name, description);
        configure(child);
        _children.Add(child);
        return this;
    }

    public CliCommandBuilder<TAppOptions> SetHandler(
        Func<CliNoOptions, CliExecutionContext<TAppOptions>, Task<CliCommandResult>> handler)
    {
        return SetHandler<CliNoOptions>(_ => { }, handler);
    }

    public CliCommandBuilder<TAppOptions> SetHandler<TOptions>(
        Action<CliOptionsBuilder<TOptions>> configure,
        Func<TOptions, CliExecutionContext<TAppOptions>, Task<CliCommandResult>> handler)
        where TOptions : class, new()
    {
        var optionsBuilder = new CliOptionsBuilder<TOptions>();
        configure(optionsBuilder);
        _handler = new CliCommandHandler<TAppOptions, TOptions>(optionsBuilder.Binders, handler);
        return this;
    }

    internal Command BuildCommand(
        CliApplication<TAppOptions> application,
        IReadOnlyList<ICliValueBinder<TAppOptions>> globalBinders,
        string path)
    {
        var command = path == Name
            ? new RootCommand(Description)
            : new Command(Name, Description);
        var commandPath = path == Name ? string.Empty : string.Join(' ', path, Name).Trim();

        if (_handler is not null)
        {
            foreach (var symbol in _handler.Symbols)
            {
                switch (symbol)
                {
                    case Option option:
                        command.Options.Add(option);
                        break;
                    case Argument argument:
                        command.Arguments.Add(argument);
                        break;
                }
            }

            command.SetAction((ParseResult parseResult, CancellationToken cancellationToken) =>
                application.ExecuteHandlerAsync(
                    commandPath,
                    _handler,
                    globalBinders,
                    parseResult,
                    cancellationToken));
        }

        foreach (var child in _children)
        {
            command.Subcommands.Add(child.BuildCommand(application, globalBinders, commandPath));
        }

        return command;
    }
}

public sealed class CliOptionsBuilder<TOptions>
    where TOptions : class, new()
{
    private readonly List<ICliValueBinder<TOptions>> _binders = [];

    internal IReadOnlyList<ICliValueBinder<TOptions>> Binders => _binders;

    public CliOptionsBuilder<TOptions> AddOption<TValue>(
        string name,
        string description,
        Action<TOptions, TValue?> assign,
        bool required = false,
        params string[] aliases)
    {
        var allAliases = CliApplicationBuilder<TOptions>.BuildAliases(name, aliases);
        var option = new Option<TValue>(allAliases[0], allAliases.Skip(1).ToArray())
        {
            Description = description,
            Required = required
        };

        _binders.Add(new CliValueBinder<TOptions, TValue>(option, assign));
        return this;
    }

    public CliOptionsBuilder<TOptions> AddArgument<TValue>(
        string name,
        string description,
        Action<TOptions, TValue?> assign)
    {
        var argument = new Argument<TValue>(name)
        {
            Description = description
        };

        _binders.Add(new CliValueBinder<TOptions, TValue>(argument, assign));
        return this;
    }
}

internal interface ICliCommandHandler<TAppOptions>
    where TAppOptions : class, new()
{
    IReadOnlyList<Symbol> Symbols { get; }

    Task<CliCommandResult> ExecuteAsync(
        ParseResult parseResult,
        CliExecutionContext<TAppOptions> context);
}

internal sealed class CliCommandHandler<TAppOptions, TOptions> : ICliCommandHandler<TAppOptions>
    where TAppOptions : class, new()
    where TOptions : class, new()
{
    private readonly IReadOnlyList<ICliValueBinder<TOptions>> _binders;
    private readonly Func<TOptions, CliExecutionContext<TAppOptions>, Task<CliCommandResult>> _handler;

    public CliCommandHandler(
        IReadOnlyList<ICliValueBinder<TOptions>> binders,
        Func<TOptions, CliExecutionContext<TAppOptions>, Task<CliCommandResult>> handler)
    {
        _binders = binders;
        _handler = handler;
        Symbols = binders.Select(x => x.Symbol).ToArray();
    }

    public IReadOnlyList<Symbol> Symbols { get; }

    public Task<CliCommandResult> ExecuteAsync(
        ParseResult parseResult,
        CliExecutionContext<TAppOptions> context)
    {
        var options = new TOptions();
        foreach (var binder in _binders)
        {
            binder.Bind(parseResult, options);
        }

        return _handler(options, context);
    }
}

internal interface ICliValueBinder<in TOptions>
    where TOptions : class
{
    Symbol Symbol { get; }

    void Bind(ParseResult parseResult, TOptions options);
}

internal sealed class CliValueBinder<TOptions, TValue> : ICliValueBinder<TOptions>
    where TOptions : class
{
    private readonly Symbol _symbol;
    private readonly Action<TOptions, TValue?> _assign;

    public CliValueBinder(Option<TValue> option, Action<TOptions, TValue?> assign)
    {
        _symbol = option;
        _assign = assign;
    }

    public CliValueBinder(Argument<TValue> argument, Action<TOptions, TValue?> assign)
    {
        _symbol = argument;
        _assign = assign;
    }

    public Symbol Symbol => _symbol;

    public void Bind(ParseResult parseResult, TOptions options)
    {
        var value = _symbol switch
        {
            Option<TValue> option => parseResult.GetValue(option),
            Argument<TValue> argument => parseResult.GetValue(argument),
            _ => default
        };

        _assign(options, value);
    }
}

internal sealed record CliRecursiveOptionSpec(IReadOnlyList<string> Aliases, bool TakesValue);
