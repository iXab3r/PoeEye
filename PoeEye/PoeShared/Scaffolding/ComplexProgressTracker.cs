namespace PoeShared.Scaffolding;

/// <summary>
/// Manages and tracks the progress of multiple tasks, providing an aggregated progress percentage.
/// </summary>
public sealed class ComplexProgressTracker : DisposableReactiveObject
{
    private static readonly IFluentLog Log = typeof(ComplexProgressTracker).PrepareLogger();

    private readonly IDictionary<string, int> progressByTask = new Dictionary<string, int>();
    public ComplexProgressTracker()
    {
            
    }
        
    /// <summary>
    /// Gets the overall progress percentage, calculated as the average of all tracked tasks' progress.
    /// </summary>
    public int ProgressPercent { get; private set; }
        
    /// <summary>
    /// Updates the progress of a specified task and recalculates the overall progress percentage.
    /// </summary>
    /// <param name="progressPercent">The progress percentage of the task.</param>
    /// <param name="taskName">The name of the task being updated.</param>
    public void Update(int progressPercent, string taskName)
    {
        progressByTask[taskName] = progressPercent;
        var totalProgress = (int)progressByTask.Values.Average();
        Log.Debug($"{taskName} is in progress: {progressPercent}%");
        ProgressPercent = totalProgress;
    }
}