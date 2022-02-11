namespace PoeShared.GCLog;

public abstract class GcLogBase : DisposableReactiveObject, IGcLog
{
    private StreamWriter fileWriter;

    protected GcLogBase(FileInfo filePath)
    {
        Log = GetType().PrepareLogger();
        Log.Debug(() => $"Initializing Gc collector, output file: {filePath}");
        FilePath = filePath;

        Disposable.Create(() =>
        {
            Log.Debug(() => "Disposing collector");
            OnStop();
            fileWriter.Flush();
            fileWriter.Dispose();
            fileWriter = null;
        });
    }
    
    public FileInfo FilePath { get; }

    private IFluentLog Log { get; }

    public void Start()
    {
        if (fileWriter != null)
        {
            throw new InvalidOperationException("Start can't be called twice: Stop must be called first.");
        }

        if (Anchors.IsDisposed)
        {
            throw new ObjectDisposedException("Collector is already disposed");
        }
        Log.Debug(() => "Starting collector");
        fileWriter = new StreamWriter(FilePath.FullName);
        OnStart();
    }

    protected bool WriteLine(string line)
    {
        if (fileWriter == null)
        {
            return false; // just in case the method is called AFTER Stop
        }

        fileWriter.WriteLine(line);
        return true;
    }

    protected abstract void OnStart();

    protected abstract void OnStop();
}