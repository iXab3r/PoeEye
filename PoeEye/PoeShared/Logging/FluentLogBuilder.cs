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

    public bool IsDebugEnabled => FluentLogSettings.Instance.MinLogLevel == null ? logWriter.IsDebugEnabled : FluentLogSettings.Instance.MinLogLevel <= FluentLogLevel.Debug;

    public bool IsInfoEnabled => FluentLogSettings.Instance.MinLogLevel == null ? logWriter.IsDebugEnabled : FluentLogSettings.Instance.MinLogLevel <= FluentLogLevel.Info;

    public bool IsWarnEnabled => FluentLogSettings.Instance.MinLogLevel == null ? logWriter.IsDebugEnabled : FluentLogSettings.Instance.MinLogLevel <= FluentLogLevel.Warn;

    public bool IsErrorEnabled => FluentLogSettings.Instance.MinLogLevel == null ? logWriter.IsDebugEnabled : FluentLogSettings.Instance.MinLogLevel <= FluentLogLevel.Error;

    public LogData Data { get; set; }
}