using System.Diagnostics;

namespace PoeShared.Scaffolding;

public sealed class OperationTimer : IDisposable
{
    private readonly ValueStopwatch sw;

    private readonly Action<TimeSpan> endAction;

    private TimeSpan previousOperationTimestamp;

    public OperationTimer(Action<TimeSpan> endAction)
    {
        this.endAction = endAction;
        sw = ValueStopwatch.StartNew();
    }

    public void PutTimestamp()
    {
        previousOperationTimestamp = sw.Elapsed;
    }

    public TimeSpan MeasureStep()
    {
        var timestamp = sw.Elapsed;
        var operationTime = timestamp - previousOperationTimestamp;
        previousOperationTimestamp = timestamp;
        return operationTime;
    }

    public void Step()
    {
        MeasureStep();
    }
    
    public void Step(Action<TimeSpan> action)
    {
        var operationTime = MeasureStep();
        action(operationTime);
    }

    public TimeSpan Elapsed => sw.Elapsed;

    public void Dispose()
    {
        endAction(sw.Elapsed);
    }
}