namespace PoeShared.Scaffolding;

public sealed class SimpleProgressReporter : DisposableReactiveObject, IProgressReporter
{
    public string TaskName { get; init; }
    
    public double Weight { get; init; }
    
    public double ProgressPercent { get; private set; }
    
    public void Update(double progressPercent)
    {
        ProgressPercent = progressPercent;
    }
}