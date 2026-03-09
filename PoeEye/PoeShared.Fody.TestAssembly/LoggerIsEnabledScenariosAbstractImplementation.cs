// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

using PoeShared.Logging;

public sealed class LoggerIsEnabledScenariosAbstractImplementation : LoggerIsEnabledScenariosAbstract
{
    public LoggerIsEnabledScenariosAbstractImplementation(IFluentLog logger) : base(logger)
    {
    }

    public override void LogInfo()
    {
        _logger.Info("message");
    }
}