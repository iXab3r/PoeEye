namespace PoeShared.Services;

internal sealed class DefaultSleepProvider : ISleepProvider
{
    public void Sleep(TimeSpan timeout, CancellationToken token = default)
    {
        token.WaitHandle.WaitOne(timeout);
    }

    public void Sleep(double timeoutMs, CancellationToken token = default)
    {
        token.WaitHandle.WaitOne((int)timeoutMs);
    }
}