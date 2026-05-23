namespace PoeShared.Services;

internal sealed class DefaultSleepProvider : ISleepProvider
{
    public void Sleep(TimeSpan timeout, CancellationToken token = default)
    {
        token.WaitHandle.WaitOne(timeout);
    }

    public void Sleep(double timeoutMs, CancellationToken token = default)
    {
        if (timeoutMs <= 0)
        {
            token.WaitHandle.WaitOne(0);
            return;
        }

        var timeout = timeoutMs >= int.MaxValue
            ? int.MaxValue
            : Math.Max(1, (int)Math.Ceiling(timeoutMs));
        token.WaitHandle.WaitOne(timeout);
    }
}
