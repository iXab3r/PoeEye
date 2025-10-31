using System;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ByteSizeLib;
using JetBrains.Annotations;
using PInvoke;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using PropertyBinder;
using ReactiveUI;

namespace PoeShared.Services;

/// <summary>
/// Provides real-time performance metrics for the current process and exposes helper operations
/// such as on-demand garbage collection. Implementations are expected to periodically sample
/// process and runtime state to keep metrics up to date and to be safe to observe from UI threads.
/// </summary>
public interface IPerformanceMetricsProvider : IDisposableReactiveObject
{
    /// <summary>
    /// Enables or disables metrics collection. When set to <c>false</c>, the implementation should pause
    /// background sampling and resume it when set back to <c>true</c>.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Indicates whether the low-level system-wide keyboard hook is currently active.
    /// </summary>
    bool SystemHookIsActive { get; }

    /// <summary>
    /// Indicates whether the in-process application keyboard hook is currently active.
    /// </summary>
    bool AppHookIsActive { get; }

    /// <summary>
    /// Percentage of total processor time used by the process since the previous sampling interval.
    /// The value is normalized by the number of logical processors (e.g., may exceed 100 on multi-core
    /// systems if not normalized; this value is already normalized to 0..100 per sampling window).
    /// </summary>
    float? ProcessorTimePercent { get; }

    /// <summary>
    /// Current working set (resident set) size of the process.
    /// </summary>
    ByteSize WorkingSet { get; }

    /// <summary>
    /// Private (non-shared) working set size of the process.
    /// </summary>
    ByteSize WorkingPrivateSet { get; }

    /// <summary>
    /// Estimated managed heap size as reported by the .NET GC.
    /// </summary>
    ByteSize Heap { get; }

    /// <summary>
    /// Estimated native (unmanaged) memory usage calculated as PrivateWorkingSet - Heap.
    /// </summary>
    ByteSize Native { get; }

    /// <summary>
    /// Amount of fragmentation in the managed heap as reported by the .NET GC.
    /// </summary>
    ByteSize Fragmented { get; }

    /// <summary>
    /// Percentage of user-mode CPU time used by the process since the previous sampling interval.
    /// </summary>
    float? UserTimePercent { get; }

    /// <summary>
    /// The number of threads currently running in the process.
    /// </summary>
    long? ThreadCount { get; }

    /// <summary>
    /// The number of open handles owned by the process.
    /// </summary>
    long? HandleCount { get; }

    /// <summary>
    /// Total number of disk write I/O operations performed by the process since start.
    /// </summary>
    ulong? DiskWriteOperationCount { get; }

    /// <summary>
    /// Total number of disk read I/O operations performed by the process since start.
    /// </summary>
    ulong? DiskReadOperationCount { get; }

    /// <summary>
    /// Cumulative number of bytes written to disk by the process since start.
    /// </summary>
    ByteSize DiskWrites { get; }

    /// <summary>
    /// Cumulative number of bytes read from disk by the process since start.
    /// </summary>
    ByteSize DiskReads { get; }

    /// <summary>
    /// Triggers an asynchronous full garbage collection. Useful to free memory under user control
    /// without blocking the calling thread.
    /// </summary>
    Task CollectGarbageAsync();

    /// <summary>
    /// Triggers a synchronous full garbage collection and logs before/after statistics for diagnostics.
    /// </summary>
    void CollectGarbage();
}

/// <inheritdoc cref="IPerformanceMetricsProvider"/>
internal sealed class PerformanceMetricsProvider : DisposableReactiveObject, IPerformanceMetricsProvider
{
    private static readonly Binder<PerformanceMetricsProvider> Binder = new();

    private static readonly IFluentLog Log = typeof(PerformanceMetricsProvider).PrepareLogger();

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetProcessIoCounters(IntPtr hProcess, out Kernel32.IO_COUNTERS lpIoCounters);

    private readonly IKeyboardEventsSource keyboardEventsSource;
    private readonly WorkerThread metricsThread;

    static PerformanceMetricsProvider()
    {
        Binder.Bind(x => x.keyboardEventsSource.SystemHookIsActive).To(x => x.SystemHookIsActive);
        Binder.Bind(x => x.keyboardEventsSource.AppHookIsActive).To(x => x.AppHookIsActive);
    }

    public PerformanceMetricsProvider(
        IApplicationAccessor applicationAccessor,
        IKeyboardEventsSource keyboardEventsSource)
    {
        this.keyboardEventsSource = keyboardEventsSource;

        metricsThread = new WorkerThread("PerformanceMetrics", HandleMetrics, autoStart: false).AddTo(Anchors);

        Observable.CombineLatest(
                applicationAccessor.WhenAnyValue(x => x.IsLoaded).Where(x => x),
                this.WhenAnyValue(x => x.IsEnabled).Where(x => x), (isLoaded, isEnabled) => (isLoaded, isEnabled))
            .Where(x => x.isEnabled && x.isLoaded)
            .Take(1)
            .Subscribe(x =>
            {
                Log.Info("Application is loaded & performance metrics are enabled - starting performance metrics thread");
                metricsThread.Start();
            })
            .AddTo(Anchors);

        this.keyboardEventsSource = keyboardEventsSource;

        Binder.Attach(this).AddTo(Anchors);
    }

    public TimeSpan SamplingPeriod { get; set; } = TimeSpan.FromSeconds(1);
    
    /// <inheritdoc />
    public bool IsEnabled { get; set; }

    /// <inheritdoc />
    public bool SystemHookIsActive { get; [UsedImplicitly] private set; }

    /// <inheritdoc />
    public bool AppHookIsActive { get; [UsedImplicitly] private set; }

    /// <inheritdoc />
    public float? ProcessorTimePercent { get; private set; }

    /// <inheritdoc />
    public ByteSize WorkingSet { get; private set; }

    /// <inheritdoc />
    public ByteSize WorkingPrivateSet { get; private set; }

    /// <inheritdoc />
    public ByteSize Heap { get; private set; }

    /// <inheritdoc />
    public ByteSize Native { get; private set; }

    /// <inheritdoc />
    public ByteSize Fragmented { get; private set; }

    /// <inheritdoc />
    public float? UserTimePercent { get; private set; }

    /// <inheritdoc />
    public long? ThreadCount { get; private set; }

    /// <inheritdoc />
    public long? HandleCount { get; private set; }

    /// <inheritdoc />
    public ulong? DiskWriteOperationCount { get; private set; }

    /// <inheritdoc />
    public ulong? DiskReadOperationCount { get; private set; }

    /// <inheritdoc />
    public ByteSize DiskWrites { get; private set; }

    /// <inheritdoc />
    public ByteSize DiskReads { get; private set; }

    private void HandleMetrics(CancellationToken cancellationToken)
    {
        try
        {
            Log.Info($"Initializing performance metrics");
            PopulateStats(cancellationToken);
        }
        catch (Exception e)
        {
            Log.Warn("Exception in metrics thread", e);
        }
    }

    private void PopulateStats(CancellationToken cancellationToken)
    {
        var previousCheckTime = DateTime.UtcNow;
        var previousCpuTime = TimeSpan.Zero;
        var previousUserCpuTime = TimeSpan.Zero;
        var startTimestamp = DateTime.UtcNow;
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!IsEnabled)
            {
                this.WhenAnyValue(x => x.IsEnabled).Where(x => x).Take(1).Wait();
            }

            using var currentProcess = Process.GetCurrentProcess();
            var now = DateTime.UtcNow;
            try
            {
                if (now - startTimestamp <= TimeSpan.FromSeconds(1))
                {
                    //skip few samples
                    continue;
                }
                
                WorkingSet = ByteSize.FromBytes(currentProcess.WorkingSet64);
                WorkingPrivateSet = ByteSize.FromBytes(currentProcess.PrivateMemorySize64);

                var gcMemoryInfo = GC.GetGCMemoryInfo();
                Heap = ByteSize.FromBytes(gcMemoryInfo.HeapSizeBytes);
                Fragmented = ByteSize.FromBytes(gcMemoryInfo.FragmentedBytes);
                Native = WorkingPrivateSet - Heap;

                var intervalLengthMilliseconds = (now - previousCheckTime).TotalMilliseconds;
                var totalAvailableCpuTime = intervalLengthMilliseconds * Environment.ProcessorCount;

                var totalCpuTimeUsedSinceLastCheck = currentProcess.TotalProcessorTime.TotalMilliseconds - previousCpuTime.TotalMilliseconds;
                var userCpuTimeUsedSinceLastCheck = currentProcess.UserProcessorTime.TotalMilliseconds - previousUserCpuTime.TotalMilliseconds;

                ProcessorTimePercent = (float) ((totalCpuTimeUsedSinceLastCheck / totalAvailableCpuTime) * 100);
                UserTimePercent = (float) ((userCpuTimeUsedSinceLastCheck / totalAvailableCpuTime) * 100);

                ThreadCount = currentProcess.Threads.Count;
                HandleCount = currentProcess.HandleCount;

                if (ProcessorTimePercent > 100)
                {
                    
                }

                if (GetProcessIoCounters(currentProcess.Handle, out var ioCounters))
                {
                    DiskReadOperationCount = ioCounters.ReadOperationCount;
                    DiskWriteOperationCount = ioCounters.WriteOperationCount;
                    DiskReads = ByteSize.FromBytes(ioCounters.ReadTransferCount);
                    DiskWrites = ByteSize.FromBytes(ioCounters.WriteTransferCount);
                }
                else
                {
                    DiskReadOperationCount = default;
                    DiskWriteOperationCount = default;
                    DiskReads = default;
                    DiskWrites = default;
                }
            }
            finally
            {
                previousCpuTime = currentProcess.TotalProcessorTime;
                previousUserCpuTime = currentProcess.UserProcessorTime;
                previousCheckTime = now;
            }

            cancellationToken.WaitHandle.WaitOne(SamplingPeriod);
        }
    }

    /// <inheritdoc />
    public void CollectGarbage()
    {
        Log.Debug("Collecting garbage");
        var before = GC.GetGCMemoryInfo();
        var statsBefore = new
        {
            before.PauseTimePercentage,
            before.HeapSizeBytes,
            before.FragmentedBytes,
            before.PromotedBytes,
            before.MemoryLoadBytes,
            before.TotalCommittedBytes,
            before.TotalAvailableMemoryBytes,
            before.HighMemoryLoadThresholdBytes,
            before.FinalizationPendingCount
        };
        Log.Debug($"GC stats BEFORE collection: {statsBefore}");
        GC.Collect();
        var after = GC.GetGCMemoryInfo();
        var statsAfter = new
        {
            after.PauseTimePercentage,
            after.HeapSizeBytes,
            after.FragmentedBytes,
            after.PromotedBytes,
            after.MemoryLoadBytes,
            after.TotalCommittedBytes,
            after.TotalAvailableMemoryBytes,
            after.HighMemoryLoadThresholdBytes,
            after.FinalizationPendingCount
        };
        Log.Debug($"GC stats AFTER collection: {statsAfter}");
        var stats = new
        {
            HeapSizeBytes = after.HeapSizeBytes - before.HeapSizeBytes,
            FragmentedBytes = after.FragmentedBytes - before.FragmentedBytes,
            PromotedBytes = after.PromotedBytes - before.PromotedBytes,
            MemoryLoadBytes = after.MemoryLoadBytes - before.MemoryLoadBytes,
            TotalCommittedBytes = after.TotalCommittedBytes - before.TotalCommittedBytes,
            TotalAvailableMemoryBytes = after.TotalAvailableMemoryBytes - before.TotalAvailableMemoryBytes,
            HighMemoryLoadThresholdBytes = after.HighMemoryLoadThresholdBytes - before.HighMemoryLoadThresholdBytes,
            FinalizationPendingCount = after.FinalizationPendingCount - before.FinalizationPendingCount
        };
        Log.Debug($"GC stats difference: {stats}");
        Log.Debug("Collected garbage");
    }

    /// <inheritdoc />
    public async Task CollectGarbageAsync()
    {
        await Task.Run(() => { CollectGarbage(); });
    }

    /// <summary>
    ///     There is a huge problem with this method - FindInstanceName(both versions) takes about 20 seconds and allocated
    ///     800MB in the process
    ///     The problem lies deep in Performance Counters lib:
    ///     22.7%   HandleMetrics  •  859,492 KB  •
    ///     EyeAuras.UI.MainWindow.ViewModels.PerformanceMetricsAddon.HandleMetrics(CancellationToken)
    ///     22.7%   FindInstanceName  •  856,653 KB  •
    ///     EyeAuras.UI.MainWindow.ViewModels.PerformanceMetricsAddon.FindInstanceName(Int32)
    ///     22.7%   PerformanceCounterCategory::GetCounterInstances  •  856,556 KB  •
    ///     System.Diagnostics.PerformanceCounter.dll!System.String[]
    ///     System.Diagnostics.PerformanceCounterCategory::GetCounterInstances(System.String, System.String)
    ///     22.7%   PerformanceCounterLib::GetCategorySample  •  856,556 KB  •
    ///     System.Diagnostics.PerformanceCounter.dll!System.Diagnostics.CategorySample
    ///     System.Diagnostics.PerformanceCounterLib::GetCategorySample(System.String, System.String)
    ///     22.7%   PerformanceCounterLib::GetCategorySample  •  856,556 KB  •
    ///     System.Diagnostics.PerformanceCounter.dll!System.Diagnostics.CategorySample
    ///     System.Diagnostics.PerformanceCounterLib::GetCategorySample(System.String)
    ///     22.6%   PerformanceCounterLib::get_CategoryTable  •  854,459 KB  •
    ///     System.Diagnostics.PerformanceCounter.dll!System.Collections.Hashtable
    ///     System.Diagnostics.PerformanceCounterLib::get_CategoryTable()
    ///     21.8%   PerformanceCounterLib::GetPerformanceData  •  825,484 KB  •
    ///     System.Diagnostics.PerformanceCounter.dll!System.Byte[]
    ///     System.Diagnostics.PerformanceCounterLib::GetPerformanceData(System.String, System.Boolean)
    ///     21.8%   PerformanceMonitor::GetData  •  825,484 KB  •  System.Diagnostics.PerformanceCounter.dll!System.Byte[]
    ///     System.Diagnostics.PerformanceMonitor::GetData(System.String, System.Boolean)
    ///     21.8%   PerformanceDataRegistryKey::GetValue  •  825,484 KB  •
    ///     System.Diagnostics.PerformanceCounter.dll!System.Byte[]
    ///     System.Diagnostics.PerformanceDataRegistryKey::GetValue(System.String, System.Boolean)
    ///     21.8%   Interop+Advapi32::RegQueryValueEx  •  825,484 KB  •  System.Diagnostics.PerformanceCounter.dll!System.Int32
    ///     Interop+Advapi32::RegQueryValueEx(Internal.Win32.SafeHandles.SafeRegistryHandle, System.String, System.Int32[],
    ///     System.Int32&, System.Byte[], System.Int32&)
    ///     21.8%   Interop+Advapi32::
    ///     <RegQueryValueEx>
    ///         g____PInvoke|1_0  •  825,484 KB  •  System.Diagnostics.PerformanceCounter.dll!System.Int32 Interop+Advapi32::
    ///         <RegQueryValueEx>
    ///             g____PInvoke|1_0(System.IntPtr, System.UInt16*, System.Int32*, System.Int32*, System.Byte*, System.Int32*)
    ///             21.8%   RegQueryValueExW  •  825,484 KB  •  KernelBase.dll!RegQueryValueExW
    ///             21.8%   BaseRegQueryValueInternal  •  825,484 KB  •  KernelBase.dll!BaseRegQueryValueInternal
    ///             21.8%   PerfRegQueryValue  •  825,484 KB  •  advapi32.dll!PerfRegQueryValue
    ///             21.8%   PerfRegQueryValueEx  •  825,484 KB  •  advapi32.dll!PerfRegQueryValueEx
    ///             15.5%   advapi32.dll  •  586,461 KB
    ///             15.5%   QueryV2Provider  •  586,461 KB  •  advapi32.dll!QueryV2Provider
    ///             15.5%   PerflibciQueryV2Provider  •  586,461 KB  •  advapi32.dll!PerflibciQueryV2Provider
    ///             14.6%   PerflibciBuildPerfObjectType  •  550,668 KB  •  advapi32.dll!PerflibciBuildPerfObjectType
    ///             14.5%   PerflibciExtendBuffer  •  547,497 KB  •  advapi32.dll!PerflibciExtendBuffer
    ///             14.5%   operator new  •  547,497 KB  •  advapi32.dll!operator new
    ///             14.5%   operator new  •  547,497 KB  •  advapi32.dll!operator new
    ///             14.5%   LocalAlloc  •  547,497 KB  •  KernelBase.dll!LocalAlloc
    ///             14.5%   ntdll.dll  •  547,497 KB
    ///             1.36%   [GC Wait]  •  51,562 KB
    ///             0.11%   [Unknown]  •  4,242 KB
    ///             0.08%   [Unknown]  •  3,068 KB
    ///             ►
    ///             <0.01%   operator new  •  103 KB  • advapi32.dll! operator new
    ///                 ► 0.42% PerflibciLocalQueryCounterData  •  15,797 KB  • advapi32.dll! PerflibciLocalQueryCounterData
    ///                 ► 0.34% advapi32.dll  •  12,697 KB
    ///                 ► 0.12% operator new  •  4,588 KB  • advapi32.dll! operator new
    ///                 ► 0.03% PerflibciLocalValidateCounters  •  1,204 KB  • advapi32.dll! PerflibciLocalValidateCounters
    ///                 ► 0.02% PerflibciEnsureCounterSetList  •  914 KB  • advapi32.dll! PerflibciEnsureCounterSetList
    ///                 ► 0.02% PerflibciBuildDefinitionBlock  •  593 KB  • advapi32.dll! PerflibciBuildDefinitionBlock
    ///                 ► 6.32% QueryExtensibleData  •  238,726 KB  • advapi32.dll! QueryExtensibleData
    ///             ► <0.01%   PerfOpenKey  •  297 KB  • advapi32.dll! PerfOpenKey
    ///                   ► 0.77% PerformanceCounterLib::get_NameTable  •  28,975 KB  •
    ///                   System.Diagnostics.PerformanceCounter.dll! System.Collections.Hashtable
    ///                   System.Diagnostics.PerformanceCounterLib::get_NameTable()
    ///                   ► 0.05% PerformanceCounterLib::GetPerformanceData  •  1,999 KB  •
    ///                   System.Diagnostics.PerformanceCounter.dll! System.Byte[]
    ///                   System.Diagnostics.PerformanceCounterLib::GetPerformanceData( System.String, System.Boolean)
    ///             ►
    ///             <0.01%   ctor  •  98 KB  • System.Diagnostics.PerformanceCounter.dll!
    ///                 System.Diagnostics.CategorySample::.ctor( System.Byte[], System.Diagnostics.CategoryEntry,
    ///                 System.Diagnostics.PerformanceCounterLib)
    ///             ►
    ///             <0.01%   PerformanceCounter::get_RawValue  •  98 KB  • System.Diagnostics.PerformanceCounter.dll!
    ///                 System.Int64 System.Diagnostics.PerformanceCounter::get_RawValue()
    ///                 ► 0.07% Info  •  2,740 KB  • PoeShared.Logging.IFluentLog.Info( Func)
    ///             <0.01%   ThePreStub  •  98 KB  • coreclr.dll! ThePreStub
    /// </summary>
    /// <param name="cancellationToken"></param>
    private async Task PopulateUsingPerformanceCounters(CancellationToken cancellationToken)
    {
        var instanceName = FindInstanceName(Environment.ProcessId);
        var processorTimeCounter = new PerformanceCounter("Process", "% Processor Time", instanceName, true);
        var userTimeCounter = new PerformanceCounter("Process", "% User Time", instanceName, true);
        var elapsedTimeCounter = new PerformanceCounter("Process", "Elapsed Time", instanceName, true);
        var threadCount = new PerformanceCounter("Process", "Thread Count", instanceName, true);

        var handleCountCounter = new PerformanceCounter("Process", "Handle Count", instanceName, true);
        var dataBytesPerSecondCounter = new PerformanceCounter("Process", "IO Data Bytes/sec", instanceName, true);

        var workingSetCounter = new PerformanceCounter("Process", "Working Set", instanceName, true);
        var workingPrivateSetCounter = new PerformanceCounter("Process", "Working Set - Private", instanceName, true);
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!IsEnabled)
            {
                this.WhenAnyValue(x => x.IsEnabled).Where(x => x).Take(1).Wait();
            }

            WorkingPrivateSet = ByteSize.FromBytes(workingPrivateSetCounter.NextSample().RawValue);
            WorkingSet = ByteSize.FromBytes(workingSetCounter.NextSample().RawValue);

            var gcMemoryInfo = GC.GetGCMemoryInfo();
            Heap = ByteSize.FromBytes(gcMemoryInfo.HeapSizeBytes);
            Fragmented = ByteSize.FromBytes(gcMemoryInfo.FragmentedBytes);
            Native = WorkingPrivateSet - Heap;

            ProcessorTimePercent = processorTimeCounter.NextValue() / Environment.ProcessorCount;
            UserTimePercent = userTimeCounter.NextValue();
            ThreadCount = (long) threadCount.NextValue();

            cancellationToken.WaitHandle.WaitOne(SamplingPeriod);
        }
    }

    private string FindInstanceNameLegacy(int processId)
    {
        Log.Debug($"Trying to find performance metrics instance name of PID {processId}");
        var process = Process.GetProcessById(processId);
        if (process == null)
        {
            throw new ArgumentException($"Failed to find process with Id {processId}");
        }

        var processName = process.ProcessName;
        Log.Debug($"Resolve process name of PID {processId} to {processName}");

        var instanceId = 0;

        var resolvedProcessId = 0;
        string candidateName = default;
        while (resolvedProcessId != processId)
        {
            candidateName = $"{processName}{(instanceId == 0 ? null : $"#{instanceId}")}";
            Log.Debug($"Resolving processId by metric name {candidateName}");
            using var processIdCounter = new PerformanceCounter("Process", "ID Process", candidateName, true);
            resolvedProcessId = (int) processIdCounter.NextSample().RawValue;
            Log.Debug($"Resolved processId by metric name {candidateName} to {resolvedProcessId}");
            instanceId++;
        }

        if (string.IsNullOrEmpty(candidateName))
        {
            throw new InvalidStateException($"Failed to resolve instanceName by processId {processId}");
        }

        Log.Debug($"Resolve process name of PID {processId} to {candidateName}");
        return candidateName;
    }

    private static string FindInstanceName(int processId)
    {
        Log.Debug($"Attempting to find the performance counter instance name for process ID {processId}.");

        try
        {
            var category = new PerformanceCounterCategory("Process");
            var instances = category.GetInstanceNames();
            foreach (var instance in instances)
            {
                try
                {
                    using var counter = new PerformanceCounter("Process", "ID Process", instance, true);
                    if ((int) counter.RawValue != processId)
                    {
                        continue;
                    }

                    Log.Debug($"Match found: Process ID {processId} has performance counter instance name '{instance}'.");
                    return instance;
                }
                catch (Exception ex)
                {
                    // This catch block ensures that if one counter fails, it doesn't break the loop
                    Log.Error($"Error reading performance counter for instance '{instance}'.", ex);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to find the performance counter instance name for process ID {processId}.", ex);
            throw new ArgumentException($"An error occurred while trying to find the performance counter instance name for process ID {processId}.", ex);
        }

        var errorMessage = $"Could not find the performance counter instance name for process ID {processId}.";
        Log.Warn(errorMessage);
        throw new ArgumentException(errorMessage);
    }
}