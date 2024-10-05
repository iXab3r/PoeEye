namespace PoeShared.Services;

public interface ISleepController
{
    ISleepProvider Provider { get; }
    
    void SetProvider(ISleepProvider provider);
}