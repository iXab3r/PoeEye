using System;
using PoeShared.Scaffolding;

namespace PoeShared.Launcher.Services;

internal sealed class LambdaLauncherMethodHandler : ILauncherMethodHandler
{
    private readonly Action<LauncherArguments> handler;

    public LambdaLauncherMethodHandler(string method, Action<LauncherArguments> handler)
    {
        Method = method;
        this.handler = handler;
    }

    public string Method { get; }

    public void Handle(LauncherArguments arguments)
    {
        handler(arguments);
    }
}