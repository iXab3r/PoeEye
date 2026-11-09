using System;
using System.Linq;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;
using Microsoft.Extensions.Logging;
using PoeShared.Logging;

namespace PoeShared.Tests.Logging;

[TestFixture]
[NonParallelizable]
public class Logger4Log4NetFixture
{
    private MemoryAppender appender;
    private Logger sink;
    private bool wasConfigured;
    private string category;

    [SetUp]
    public void SetUp()
    {
        category = $"{GetType().FullName}.{Guid.NewGuid():N}";
        var repository = LogManager.GetAllRepositories().FirstOrDefault() ?? LogManager.GetRepository(GetType().Assembly);
        sink = (Logger)LogManager.GetLogger(repository.Name, category).Logger;
        wasConfigured = sink.Hierarchy.Configured;
        sink.Hierarchy.Configured = true;
        sink.Level = Level.All;
        sink.Additivity = false;
        appender = new MemoryAppender();
        sink.AddAppender(appender);
    }

    [TearDown]
    public void TearDown()
    {
        sink.RemoveAppender(appender);
        appender.Close();
        sink.Hierarchy.Configured = wasConfigured;
    }

    /// <summary>
    /// WHAT: Protects the regression where the default Trace minimum discarded application diagnostics, including errors.
    /// HOW: Writes through the provider's ILogger and checks the real log4net sink for severity, message and exception.
    /// </summary>
    [TestCase(LogLevel.Trace, "DEBUG")]
    [TestCase(LogLevel.Debug, "DEBUG")]
    [TestCase(LogLevel.Information, "INFO")]
    [TestCase(LogLevel.Warning, "WARN")]
    [TestCase(LogLevel.Error, "ERROR")]
    [TestCase(LogLevel.Critical, "ERROR")]
    public void ShouldDeliverDiagnosticsAtDefaultMinimum(LogLevel level, string expectedLevel)
    {
        // Given
        using var provider = new Log4NetLoggerProvider();
        var logger = provider.CreateLogger(category);
        var exception = new InvalidOperationException("Diagnostic exception");

        // When
        logger.Log(level, new EventId(1858), "Diagnostic message", exception, (message, _) => message);

        // Then
        var entry = appender.GetEvents().ShouldHaveSingleItem();
        entry.Level.Name.ShouldBe(expectedLevel);
        entry.RenderedMessage.ShouldBe("Diagnostic message");
        entry.ExceptionObject.ShouldBeSameAs(exception);
    }

    /// <summary>
    /// WHAT: A configured minimum preserves diagnostics at that severity and above.
    /// HOW: Configures the provider before creating ILogger and captures a supported message in log4net.
    /// </summary>
    [TestCase(LogLevel.Warning, LogLevel.Warning)]
    [TestCase(LogLevel.Warning, LogLevel.Error)]
    [TestCase(LogLevel.Error, LogLevel.Critical)]
    public void ShouldDeliverDiagnosticsAtConfiguredMinimum(LogLevel minimum, LogLevel level)
    {
        // Given
        using var provider = new Log4NetLoggerProvider { MinimumLevel = minimum };
        var logger = provider.CreateLogger(category);

        // When
        logger.Log(level, "Configured diagnostic");

        // Then
        appender.GetEvents().ShouldHaveSingleItem().RenderedMessage.ShouldBe("Configured diagnostic");
    }
}
