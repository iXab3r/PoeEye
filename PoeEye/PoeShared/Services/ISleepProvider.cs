namespace PoeShared.Services;

public interface ISleepProvider
{
    void Sleep(TimeSpan timeout, CancellationToken token = default);
    
    void Sleep(double timeoutMs, CancellationToken token = default);
}