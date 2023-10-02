namespace PoeShared.Scaffolding;

public sealed class ComplexProgressTracker : DisposableReactiveObject
{
    private static readonly IFluentLog Log = typeof(ComplexProgressTracker).PrepareLogger();

    private readonly IDictionary<string, int> progressByTask = new Dictionary<string, int>();
    public ComplexProgressTracker()
    {
            
    }
        
    public int ProgressPercent { get; private set; }
        
    public void Update(int progressPercent, string taskName)
    {
        progressByTask[taskName] = progressPercent;
        var totalProgress = (int)progressByTask.Values.Average();
        Log.Debug(() => $"{taskName} is in progress: {progressPercent}%");
        ProgressPercent = totalProgress;
    }
}