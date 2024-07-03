namespace PoeShared.Launcher.Services;

internal interface ILauncherMethodHandler
{
    public string Method { get; }

    public void Handle(LauncherArguments arguments);
}