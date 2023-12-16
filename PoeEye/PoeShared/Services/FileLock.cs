using System.Diagnostics;
using Polly;

namespace PoeShared.Services;

public sealed class FileLock : DisposableReactiveObject
{
    private static readonly Process CurrentProcess = Process.GetCurrentProcess();

    private readonly Stream fileStream;

    public FileLock(FileInfo lockFile) : this(lockFile, TimeSpan.Zero)
    {
    }
    
    public FileLock(FileInfo lockFile, TimeSpan timeout)
    {
        Log = GetType().PrepareLogger().WithSuffix(ToString);
        LockFile = lockFile;
        Timeout = timeout;
        ExistedInitially = Exists;
        Log.Debug(() => $"Preparing lock file, existed since start: {ExistedInitially}");
        fileStream = PrepareLockFileSafe(Log, LockFile.FullName, Timeout).AddTo(Anchors);
        Disposable.Create(() => CleanupLockFile(Log, LockFile.FullName)).AddTo(Anchors);
        Log.Debug(() => $"File lock created successfully");
    }
        
    private IFluentLog Log { get; }
        
    public FileInfo LockFile { get; }

    public bool ExistedInitially { get; }

    public bool Exists => File.Exists(LockFile.FullName);

    public TimeSpan Timeout { get; }

    protected override void FormatToString(ToStringBuilder builder)
    {
        base.FormatToString(builder);
        builder.Append(nameof(FileLock));
        builder.AppendParameter(nameof(LockFile), LockFile.FullName);
        builder.AppendParameter(nameof(ExistedInitially), ExistedInitially);
        builder.AppendParameter(nameof(Exists), Exists);
    }

    private static void CleanupLockFile(IFluentLog log, string lockFilePath)
    {
        log.Debug(() => $"Preparing to remove lock file: {lockFilePath}");
        var lockFileExists = File.Exists(lockFilePath);
        if (lockFileExists)
        {
            log.Info(() => $"Removing lock file {lockFilePath}");
            File.Delete(lockFilePath);
        }
        else
        {
            log.Warn($"Lock file {lockFilePath} does not exist for some reason");
        }

        if (File.Exists(lockFilePath))
        {
            throw new ApplicationException("Failed to remove lock file");
        }
        else
        {
            log.Info(() => $"Removed lock file {lockFilePath}");
        }
    }

    private static Stream PrepareLockFileSafe(IFluentLog log, string lockFilePath, TimeSpan timeout)
    {
        var retryCount = 10;
        var attemptTimeout = timeout / retryCount;
        var result = Policy
            .Handle<Exception>()
            .WaitAndRetry(
                retryCount,
                retryAttempt => attemptTimeout,
                (exception, timeSpan, context) => { log.WithPrefix($"#{context.Count}/{retryCount}").Debug($"Failed to open file - {exception.Message}"); }
            ).ExecuteAndCapture(context => PrepareLockFile(log, lockFilePath), new Context());
        if (result.Outcome == OutcomeType.Failure)
        {
            throw result.FinalException;
        }
        return result.Result;
    }
    
    private static Stream PrepareLockFile(IFluentLog log, string lockFilePath)
    {
        log.Debug(() => $"Creating lock file: {lockFilePath}");
        var lockFileData = $"pid: {CurrentProcess.Id}, start time: {CurrentProcess.StartTime}";
        var lockFile = new FileInfo(lockFilePath);
        if (lockFile.Directory == null)
        {
            throw new ArgumentException($"Lock file {lockFilePath} directory is not set");
        }
        if (!lockFile.Directory.Exists)
        {
            log.Debug(() => $"Creating directory for lock file: {lockFile.Directory.FullName}");
            lockFile.Directory.Create();
        }
        var lockFileStream = new FileStream(lockFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        var lockFileWriter = new StreamWriter(lockFileStream);
        log.Debug(() => $"Filling lock file with data: {lockFileData}");
        lockFileWriter.Write(lockFileStream);
        return lockFileStream;
    }
}