using ReactiveUI;

namespace PoeShared.Scaffolding;

/// <summary>
/// Manages and tracks the progress of multiple tasks, providing an aggregated progress percentage.
/// </summary>
public sealed class ComplexProgressTracker : DisposableReactiveObject
{
    private static readonly IFluentLog Log = typeof(ComplexProgressTracker).PrepareLogger();

    private readonly ConcurrentDictionary<string, ProgressReporter> progressByTask = new();
    
    public ComplexProgressTracker()
    {
            
    }

    public ComplexProgressTracker(IProgressReporter progressReporter)
    {
        this.WhenAnyValue(x => x.ProgressPercent)
            .Subscribe(x => progressReporter.Update(x))
            .AddTo(Anchors);
    }

    /// <summary>
    /// Gets the overall progress percentage, calculated as the average of all tracked tasks' progress.
    /// </summary>
    public double ProgressPercent { get; private set; }
    
    /// <summary>
    /// Last reported task name
    /// </summary>
    public string TaskName { get; private set; }

    public void Reset()
    {
        progressByTask.Clear();
        ProgressPercent = 0;
        TaskName = default;
    }
        
    /// <summary>
    /// Updates the progress of a specified task and recalculates the overall progress percentage.
    /// </summary>
    /// <param name="progressPercent">The progress percentage of the task.</param>
    /// <param name="taskName">The name of the task being updated.</param>
    public void Update(double progressPercent, string taskName)
    {
        var reporter = GetOrAdd(taskName);
        reporter.Update(progressPercent);
    }

    private void Update(ProgressReporter reporter)
    {
        var totalWeight = progressByTask.Values.Sum(x => x.Weight);
        var weightedProgressSum = progressByTask.Values
            .Sum(x => x.ProgressPercent * x.Weight);
        var totalProgress = totalWeight > 0 ? weightedProgressSum / totalWeight : 0;

        Log.Debug($"{reporter.TaskName} reported progress: {reporter.ProgressPercent}%, total: {totalProgress}%");
        TaskName = reporter.TaskName;
        ProgressPercent = totalProgress;
    }

    public IProgressReporter GetOrAdd(string taskName, double weight = 1)
    {
        return progressByTask.GetOrAdd(taskName, name => new ProgressReporter(this, name, weight));
    }

    private sealed class ProgressReporter : DisposableReactiveObject, IProgressReporter
    {
        private readonly ComplexProgressTracker progressTracker;
        
        public ProgressReporter(ComplexProgressTracker progressTracker, string taskName, double weight)
        {
            TaskName = taskName;
            Weight = weight;
            this.progressTracker = progressTracker;
            Update(0);
        }

        public string TaskName { get; }
        
        public double Weight { get; }
        
        public double ProgressPercent { get; private set; }

        public void Update(double progressPercent)
        {
            ProgressPercent = progressPercent;
            progressTracker.Update(this);
        }
    }
}