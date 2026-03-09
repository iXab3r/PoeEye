// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

using PoeShared.Logging;

public abstract class LoggerIsEnabledScenariosAbstract
{
    protected readonly IFluentLog _logger;

    public LoggerIsEnabledScenariosAbstract(IFluentLog logger)
    {
        _logger = logger;
    }

    public void LogDebug()
    {
        _logger.Debug("message");
    }

    public abstract void LogInfo();
}