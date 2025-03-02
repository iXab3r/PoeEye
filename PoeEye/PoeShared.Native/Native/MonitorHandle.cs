using System;
using PInvoke;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Native;

public sealed class MonitorHandle : IMonitorHandle
{
    private readonly Lazy<string> titleSupplier;

    public MonitorHandle(IntPtr handle)
    {
        Log = typeof(MonitorHandle).PrepareLogger().WithSuffix(() => $"hMonitor {handle.ToHexadecimal()}, Title {Title}");

        Handle = handle;
        
        titleSupplier = new Lazy<string>(() =>
        {
            try
            {
                var windowTitle = UnsafeNative.GetWindowTitle(Handle);
                return windowTitle ?? string.Empty;
            }
            catch (InvalidOperationException)
            {
                //could be not enough permissions
            }
            catch (ArgumentException)
            {
                //invalid(obsolete process id) or process is already terminated
            }
            catch (Exception ex)
            {
                if (Log.IsWarnEnabled)
                {
                    Log.Warn($"Failed to get monitor title, last error: {Kernel32.GetLastError()}", ex);
                }
            }
            return string.Empty;
        });
    }

    private IFluentLog Log { get; }
    
    public string Title => titleSupplier.Value;

    public IntPtr Handle { get; }
    
    private bool Equals(MonitorHandle other)
    {
        return Handle == other.Handle;
    }

    public override bool Equals(object obj)
    {
        return ReferenceEquals(this, obj) || obj is MonitorHandle other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Handle.GetHashCode();
    }
}