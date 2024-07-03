using System;
using System.Collections.Concurrent;
using System.Reactive.Disposables;
using CommandLine;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using Parser = CommandLine.Parser;

namespace PoeShared.Launcher.Services;

internal sealed class LauncherServiceHandler : ILauncherServiceHandler
{
    private static readonly IFluentLog Log = typeof(LauncherServiceHandler).PrepareLogger();

    private readonly ConcurrentDictionary<string, ILauncherMethodHandler> handlersByMethod = new();

    public LauncherServiceHandler()
    {
    }

    public IDisposable AddHandler<T>(string method, Action<T> handler)
    {
        return AddHandler(new LambdaLauncherMethodHandler(method, x => Adapt(handler)));
    }

    public IDisposable AddHandler(string method, Action<LauncherArguments> handler)
    {
        return AddHandler(new LambdaLauncherMethodHandler(method, handler));
    }
    
    public IDisposable AddHandler(ILauncherMethodHandler launcherMethodHandler)
    {
        handlersByMethod[launcherMethodHandler.Method] = launcherMethodHandler;
        return Disposable.Create(() =>
        {
            handlersByMethod.TryRemove(launcherMethodHandler.Method, out var _);
        });
    }

    public bool TryHandle(LauncherArguments args)
    {
        Log.Debug($"Processing request, args: {args}");
        if (!handlersByMethod.TryGetValue(args.Method, out var handler))
        {
            Log.Debug($"Request ignore - method '{args.Method}' not known");
            return false;
        }
        Log.Debug($"Handling request using handler: {handler}");
        handler.Handle(args);
        Log.Debug($"Handled request successfully");
        return true;
    }

    private static void Adapt<T>(Action<T> handler)
    {
        var arguments = Environment.GetCommandLineArgs();

        var result = new Parser(config => config.IgnoreUnknownArguments = true)
            .ParseArguments<T>(arguments);
        if (result is NotParsed<T> notParsed)
        {
            Log.Warn($"Failed to parse method arguments, type: {typeof(T)} {arguments.DumpToString()}, result: {new { notParsed.Tag, notParsed.TypeInfo, Errors = notParsed.Errors.DumpToString() }}");
            throw new ArgumentException($"Failed to parse method arguments, result: {notParsed}");
        }
        if (result is not Parsed<T> parsed)
        {
            Log.Warn($"Failed to parse method arguments to type {typeof(T)} {arguments.DumpToString()}");
            throw new ArgumentException($"Failed to parse method arguments");
        }

        handler(parsed.Value);
    }
}