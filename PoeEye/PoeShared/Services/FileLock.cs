using System.Diagnostics;

namespace PoeShared.Services;

public sealed class FileLock : DisposableReactiveObject
{
    private static readonly Process CurrentProcess = Process.GetCurrentProcess();

    private readonly Stream fileStream;

    public FileLock(FileInfo lockFile)
    {
        Log = GetType().PrepareLogger().WithSuffix(ToString);
        LockFile = lockFile;
        ExistedInitially = Exists;
        Log.Debug(() => $"Preparing lock file, existed since start: {ExistedInitially}");
        fileStream = PrepareLockFile(Log, LockFile.FullName).AddTo(Anchors);
        Disposable.Create(() => CleanupLockFile(Log, LockFile.FullName)).AddTo(Anchors);
        Log.Debug(() => $"File lock created successfully");
    }
        
    private IFluentLog Log { get; }
        
    public FileInfo LockFile { get; }

    public bool ExistedInitially { get; }

    public bool Exists => File.Exists(LockFile.FullName);

    public override string ToString()
    {
        return $"FileLock: {LockFile.Name}";
    }

    private static void CleanupLockFile(IFluentLog log, string lockFilePath)
    {
        log.Debug(() => $"Preparing to remove lock file: {lockFilePath}");
        var lockFileExists = File.Exists(lockFilePath);
        if (lockFileExists)
        {
            log.Info($"Removing lock file {lockFilePath}");
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
    }

    private static Stream PrepareLockFile(IFluentLog log, string lockFilePath)
    {
        log.Debug(() => $"Creating lock file: {lockFilePath}");
        var lockFileData = $"pid: {CurrentProcess.Id}, start time: {CurrentProcess.StartTime}";
        var lockFileStream = new FileStream(lockFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        var lockFileWriter = new StreamWriter(lockFileStream);
        log.Debug(() => $"Filling lock file with data: {lockFileData}");
        lockFileWriter.Write(lockFileStream);
        return lockFileStream;
    }
}