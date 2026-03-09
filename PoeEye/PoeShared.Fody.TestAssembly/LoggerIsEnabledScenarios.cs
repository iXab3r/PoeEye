// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

using System;
using PoeShared.Logging;

public class LoggerIsEnabledScenarios
{
    private static readonly IFluentLog StaticLogger = default;
    private readonly IFluentLog logger;
    private IFluentLog anotherLogger;

    public LoggerIsEnabledScenarios(IFluentLog logger)
    {
        this.logger = logger;
    }

    public IFluentLog GetLogger()
    {
        return logger;
    }

    public void LogDebugViaStaticLogger()
    {
        StaticLogger.Debug("message");
    }

    public void LogDebugViaGetLogger()
    {
        GetLogger().Debug("message");
    }

    public void LogDebug()
    {
        logger.Debug("message");
    }
    
    public void LogDebugWithException()
    {
        logger.Debug("message", new ApplicationException("ex"));
    }
    
    public void LogDebugFormatted()
    {
        // ReSharper disable once RedundantStringInterpolation
        logger.Debug($"message {1+1}");
    }
    
    public void LogDebugFormattedWithIntCondition()
    {
        logger.Debug($"message {(AreEqual(1,2) ? "1 <> 2" : "1 == 2")}");
    }
    
    public void LogDebugFormattedWithLogLevelCondition()
    {
        var log = logger.WithSuffix("test");
        log.Debug($"message {(AreEqual(true, logger.IsDebugEnabled) ? "LogLevel is Debug" : $"IsDebugEnabled: {logger.IsDebugEnabled}")}");
    }
    
    public void LogDebugFormattedOneArg()
    {
        logger.Debug($"message {this}");
    }
    
    public void LogDebugFormattedTwoArgs()
    {
        logger.Debug($"message {this} {this}");
    }
    
    public void LogDebugFormattedOneArgWithException()
    {
        logger.Debug($"message {this}", new ApplicationException("ex"));
    }
    
    public void LogDebugFormattedSelf()
    {
        logger.Debug($"message {logger.IsDebugEnabled} {logger.IsInfoEnabled}  {logger.IsWarnEnabled}  {logger.IsErrorEnabled}");
    }
    
    public void LogDebugViaVariable()
    {
        // ReSharper disable once InlineTemporaryVariable
        var i = 2;
        var fluentLog = logger;
        i += 1;
        fluentLog.Debug("test");

        if (i != 3)
        {
            throw new ApplicationException();
        }
    }
    
    public void LogDebugWithIf()
    {
        if (logger != StaticLogger)
        {
            logger.Debug("test");
        }
    }
    
    public void LogDebugWithStoreToField()
    {
        anotherLogger = logger;
        logger.Debug("test");
    }
    
    public void LogDebugWithStoreToFieldAndRestore()
    {
        anotherLogger = logger;
        anotherLogger.Debug("test");
    }
    
    public void LogDebugWithExceptionViaVariable()
    {
        // ReSharper disable once InlineTemporaryVariable
        var fluentLog = logger;
        fluentLog.Debug("test", new ApplicationException("ex"));
    }
    
    public void LogDebugFormattedWithExceptionViaVariable()
    {
        // ReSharper disable once InlineTemporaryVariable
        var fluentLog = logger;
        fluentLog.Debug($"test with exception {1 + 1}", new ApplicationException("ex"));
    }

    public void LogInformation()
    {
        logger.Info("message");
    }

    public void LogWarning()
    {
        logger.Warn("message");
    }

    public void LogError()
    {
        logger.Error("message");
    }
    
    public void LogDebugWithEnabledProperty()
    {
        if (logger.IsDebugEnabled)
        {
            logger.Debug("message");
        }
    }

    public void LogDebugWithEnabled()
    {
        if (logger.IsEnabled(FluentLogLevel.Debug))
        {
            logger.Debug("message");
        }
    }

    public void LogDebugWithEnabled_With_Code_Before()
    {
        if (logger.IsEnabled(FluentLogLevel.Debug))
        {
            CodeBefore();
            logger.Debug("message");
        }
    }

    public void LogDebugWithEnabled_With_Code_After()
    {
        if (logger.IsEnabled(FluentLogLevel.Debug))
        {
            logger.Debug("message");
            CodeAfter();
        }
    }

    public void LogDebugWithEnabled_With_Code_Before_And_After()
    {
        if (logger.IsEnabled(FluentLogLevel.Debug))
        {
            CodeBefore();
            logger.Debug("message");
            CodeAfter();
        }
    }

    public void LogDebug_With_Code_Before()
    {
        CodeBefore();
        logger.Debug("message");
    }

    public void LogDebug_With_Code_After()
    {
        logger.Debug("message");
        CodeAfter();
    }

    public void LogDebug_With_Code_Before_And_After()
    {
        CodeBefore();
        logger.Debug("message");
        CodeAfter();
    }

    public void LogDebug_Multiple()
    {
        logger.Debug("message");
        logger.Debug("message");
    }

    public void LogDebug_Multiple_With_First_IsEnabled()
    {
        if (logger.IsEnabled(FluentLogLevel.Debug))
        {
            logger.Debug("message");
        }
        logger.Debug("message");
    }

    public void LogDebug_Multiple_With_Second_IsEnabled()
    {
        logger.Debug("message");
        if (logger.IsEnabled(FluentLogLevel.Debug))
        {
            logger.Debug("message");
        }
    }

    public void LogDebug_Multiple_With_Code()
    {
        CodeBefore();
        logger.Debug("message");
        CodeBefore();
        logger.Debug("message");
    }

    public void LogDebug_In_Switch()
    {
        var number = 2 / 2; // trick compiler to not remove unreachable code
        switch (number)
        {
            case 0:
                CodeBefore();
                break;

            case 1:
                logger.Debug("message");
                break;

            default:
                CodeAfter();
                break;
        }
    }

    public void LogDebug_In_Switch2()
    {
        var number = 2 / 2; // trick compiler to not remove unreachable code
        switch (number)
        {
            case 0:
                CodeBefore();
                break;

            case 1:
                if (logger.IsEnabled(FluentLogLevel.Debug))
                {
                    logger.Debug("message");
                }
                break;

            default:
                CodeAfter();
                break;
        }
    }

    public void LogDebug_In_Switch_With_Before_Code()
    {
        var number = 2 / 2;  // trick compiler to not remove unreachable code
        switch (number)
        {
            case 0:
                CodeBefore();
                break;

            case 1:
                CodeBefore();
                logger.Debug("message");
                break;

            default:
                CodeAfter();
                break;
        }
    }

    public void LogDebug_In_Switch_With_Before_Code2()
    {
        var number = 2 / 2;  // trick compiler to not remove unreachable code
        switch (number)
        {
            case 0:
                CodeBefore();
                break;

            case 1:
                CodeBefore();
                if (logger.IsEnabled(FluentLogLevel.Debug))
                {
                    logger.Debug("message");
                }
                break;

            default:
                CodeAfter();
                break;
        }
    }

    public void LogDebug_In_Switch_With_After_Code()
    {
        var number = 2 / 2;  // trick compiler to not remove unreachable code
        switch (number)
        {
            case 0:
                CodeBefore();
                break;

            case 1:
                logger.Debug("message");
                CodeAfter();
                break;

            default:
                CodeAfter();
                break;
        }
    }

    public void LogDebug_In_Switch_With_After_Code2()
    {
        var number = 2 / 2;  // trick compiler to not remove unreachable code
        switch (number)
        {
            case 0:
                CodeBefore();
                break;

            case 1:
                if (logger.IsEnabled(FluentLogLevel.Debug))
                {
                    logger.Debug("message");
                }
                CodeAfter();
                break;

            default:
                CodeAfter();
                break;
        }
    }

    public void LogDebug_In_Switch_With_Before_And_After_Code()
    {
        var number = 2 / 2;  // trick compiler to not remove unreachable code
        switch (number)
        {
            case 0:
                CodeBefore();
                break;

            case 1:
                CodeBefore();
                logger.Debug("message");
                CodeAfter();
                break;

            default:
                CodeAfter();
                break;
        }
    }

    public void LogDebug_In_Switch_With_Before_And_After_Code2()
    {
        var number = 2 / 2;  // trick compiler to not remove unreachable code
        switch (number)
        {
            case 0:
                CodeBefore();
                break;

            case 1:
                CodeBefore();
                if (logger.IsEnabled(FluentLogLevel.Debug))
                {
                    logger.Debug("message");
                }
                CodeAfter();
                break;

            default:
                CodeAfter();
                break;
        }
    }

#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception.

    public void LogDebug_In_Exception()
    {
        try
        {
            logger.Debug("message");
        }
        catch (Exception)
        {
        }
    }

    public void LogDebug_In_Exception_With_Before_Code()
    {
        CodeBefore();
        try
        {
            logger.Debug("message");
        }
        catch (Exception)
        {
        }
    }

    public void LogDebug_In_Exception_With_After_Code()
    {
        try
        {
            logger.Debug("message");
        }
        catch (Exception)
        {
        }

        CodeAfter();
    }

    public void LogDebug_In_Exception_With_Before_And_After_Code()
    {
        CodeBefore();
        try
        {
            logger.Debug("message");
        }
        catch (Exception)
        {
        }

        CodeAfter();
    }

    public void LogDebug_In_Exception_With_Before_Code_In_Try()
    {
        try
        {
            CodeBefore();
            logger.Debug("message");
        }
        catch (Exception)
        {
        }
    }

    public void LogDebug_In_Exception_With_After_Code_In_Try()
    {
        try
        {
            logger.Debug("message");
            CodeAfter();
        }
        catch (Exception)
        {
        }
    }

    public void LogDebug_In_Exception_With_Before_And_After_Code_In_Try()
    {
        try
        {
            CodeBefore();
            logger.Debug("message");
            CodeAfter();
        }
        catch (Exception)
        {
        }
    }

#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception.

    public void LogDebug_In_Exception_Catch()
    {
        try
        {
            CodeThrowException();
        }
        catch (Exception)
        {
            logger.Debug("message");
        }
    }

    private void CodeBefore()
    {
        // some random code to fill in
        string z = null;
        for (int i = 0; i < 10; i++)
        {
            if (i % 2 == 0)
            {
                z += i.ToString();
            }
        }
    }

    private void CodeAfter()
    {
        // some random code to fill in
        string z = null;
        for (int i = 0; i < 10; i++)
        {
            if (i % 2 == 0)
            {
                z += i.ToString();
            }
        }
    }

    private void CodeThrowException()
    {
        CodeBefore();
        throw new Exception();
    }
    
    private bool AreEqual(bool first, bool second)
    {
        return first == second;
    }
    
    private bool AreEqual(int first, int second)
    {
        return first == second;
    }
}