using System;

namespace PoeShared.Launcher.Services;

internal interface ILauncherServiceHandler
{
    IDisposable AddHandler(ILauncherMethodHandler handler);
    
    IDisposable AddHandler(string method, Action<LauncherArguments> handler);

    bool TryHandle(LauncherArguments args);
}