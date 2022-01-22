using System.Diagnostics;
using App.Metrics;
using App.Metrics.Gauge;

namespace PoeShared.Scaffolding;

public static class MetricsExtensions
{
    public static void SetTime(this IMeasureGaugeMetrics metrics, string gaugeName, double value)
    {
        var gauge = new GaugeOptions() { Name = gaugeName, MeasurementUnit = Unit.Custom("ms") };
        metrics.SetValue(gauge, value);
    }
        
    public static IDisposable Time(this IMeasureGaugeMetrics metrics, string gaugeName)
    {
        return metrics.Time(new GaugeOptions() { Name = gaugeName, MeasurementUnit = Unit.Custom("ms") });
    }
        
    public static IDisposable Time(this IMeasureGaugeMetrics metrics, GaugeOptions options)
    {
        var initial = Stopwatch.StartNew();
        return Disposable.Create(() => metrics.SetValue(options, initial.ElapsedMilliseconds));
    }
}