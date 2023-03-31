using App.Metrics;

namespace PoeShared.Logging;

internal sealed class FluentLogBuilder : IFluentLog
{
    private readonly ILogWriter logWriter;

    public FluentLogBuilder(ILogWriter logWriter, LogData data = default)
    {
        Data = data;
        this.logWriter = logWriter;
    }

    ILogWriter IFluentLog.Writer => logWriter;
        
    public IMetrics Metrics => App.Metrics.Metrics.Instance ?? throw new InvalidOperationException("Metrics are not initialized yet");

    public bool IsDebugEnabled =>  logWriter.IsDebugEnabled && (FluentLogSettings.Instance.MinLogLevel ?? default) <= FluentLogLevel.Debug;

    public bool IsInfoEnabled =>  logWriter.IsInfoEnabled && (FluentLogSettings.Instance.MinLogLevel ?? default) <= FluentLogLevel.Info;

    public bool IsWarnEnabled => logWriter.IsWarnEnabled && (FluentLogSettings.Instance.MinLogLevel ?? default) <= FluentLogLevel.Warn;

    public bool IsErrorEnabled => logWriter.IsErrorEnabled && (FluentLogSettings.Instance.MinLogLevel ?? default) <= FluentLogLevel.Error;

    public LogData Data { get; set; }
}