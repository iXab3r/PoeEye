using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Disposables;
using System.Threading;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using Splat;

namespace PoeShared.Squirrel.Scaffolding;

internal sealed class SingleGlobalInstance : IDisposable, IEnableLogger
{
    private static readonly IFluentLog Log = typeof(SingleGlobalInstance).PrepareLogger();

    private IDisposable handle;

    public SingleGlobalInstance(string key, TimeSpan timeOut)
    {
        if (ModeDetector.InUnitTestRunner())
        {
            return;
        }

        var path = Path.Combine(Path.GetTempPath(), ".squirrel-lock-" + key);

        var st = Stopwatch.StartNew();

        Log.Debug($"Acquiring update lock @ {path}");
        var fh = default(FileStream);
        while (st.Elapsed < timeOut)
        {
            try
            {
                fh = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Delete);
                fh.Write(new byte[] {0xba, 0xad, 0xf0, 0x0d}, 0, 4);
                break;
            }
            catch (Exception ex)
            {
                Log.Warn("Failed to grab lockfile, will retry: " + path, ex);
                Thread.Sleep(250);
            }
        }

        st.Stop();

        if (fh == null)
        {
            throw new Exception("Couldn't acquire lock, is another instance running");
        }

        handle = Disposable.Create(
            () =>
            {
                Log.Debug($"Releasing update lock @ {path}");
                fh.Dispose();
                File.Delete(path);
            });
    }

    public void Dispose()
    {
        if (ModeDetector.InUnitTestRunner())
        {
            return;
        }

        var disp = Interlocked.Exchange(ref handle, null);
        disp?.Dispose();
    }

    ~SingleGlobalInstance()
    {
        if (handle == null)
        {
            return;
        }

        throw new AbandonedMutexException("Leaked a Mutex!");
    }
}