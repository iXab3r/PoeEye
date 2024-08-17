namespace PoeShared.Scaffolding;

public interface IProgressReporter
{
    string TaskName { get; }
    
    double Weight { get; }
    
    double ProgressPercent { get; }
    
    /// <summary>
    /// Updates the progress of a specified task and recalculates the overall progress percentage.
    /// </summary>
    /// <param name="progressPercent">The progress percentage of the task.</param>
    void Update(double progressPercent);

    void Update(int current, int total)
    {
        this.Update(((double)current / total) * 100);
    }
    
    void Update(long current, long total)
    {
        this.Update(((double)current / total) * 100);
    }
}