using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using PInvoke;
using PoeShared.Logging;
using PoeShared.Scaffolding; 
using Win32Exception = System.ComponentModel.Win32Exception;
// ReSharper disable CA1416

namespace PoeShared.Native;

internal sealed class WindowHandle : IWindowHandle  
{
    private readonly Lazy<string> classSupplier;
    private readonly Lazy<Icon> iconSupplier;
    private readonly Lazy<BitmapSource> iconBitmapSupplier;
    private readonly Lazy<IWindowHandle> ownerSupplier;
    private readonly Lazy<IWindowHandle> parentSupplier;
    private readonly Lazy<User32.WindowStyles> windowStyle;
    private readonly Lazy<User32.WindowStylesEx> windowStyleEx;
    private readonly Lazy<(int processId, int threadId)> processIdSupplier;
    private readonly Lazy<int> parentProcessIdSupplier;
    private readonly Lazy<(string processName, string processPath, string commandLine, string processArgs, DateTime createdAt)> processDataSupplier;
        
    public WindowHandle(IntPtr handle)
    {
        Log = typeof(WindowHandle).PrepareLogger().WithSuffix(() => $"HWND {handle.ToHexadecimal()}, Title {Title}, Class: {Class}");
        Handle = handle;
        Title = UnsafeNative.GetWindowTitle(handle);

        processIdSupplier = new Lazy<(int processId, int threadId)>(() =>
        {
            var threadId = 0;
            var processId = 0;
            try
            {
                threadId = User32.GetWindowThreadProcessId(handle, out processId);
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to get processId via {nameof(User32.GetWindowThreadProcessId)}, last error: {Kernel32.GetLastError()}", ex);
            } 

            return (processId: processId, threadId: threadId);
        });

        ownerSupplier = new Lazy<IWindowHandle>(() =>
        {
            var ownerHandle = User32.GetWindow(Handle, User32.GetWindowCommands.GW_OWNER);
            return ownerHandle == IntPtr.Zero ? default : new WindowHandle(ownerHandle);
        }, LazyThreadSafetyMode.ExecutionAndPublication);
        parentSupplier = new Lazy<IWindowHandle>(() =>
        {
            var parentHandle = UnsafeNative.GetParent(Handle);
            return parentHandle == IntPtr.Zero ? default : new WindowHandle(parentHandle);
        }, LazyThreadSafetyMode.ExecutionAndPublication);
        classSupplier = new Lazy<string>(() => UnsafeNative.GetWindowClass(handle), LazyThreadSafetyMode.ExecutionAndPublication);
        iconSupplier = new Lazy<Icon>(() => GetWindowIcon(handle), LazyThreadSafetyMode.ExecutionAndPublication);
        windowStyle = new Lazy<User32.WindowStyles>(() => (User32.WindowStyles)User32.GetWindowLong(handle, User32.WindowLongIndexFlags.GWL_STYLE));
        windowStyleEx = new Lazy<User32.WindowStylesEx>(() => (User32.WindowStylesEx)User32.GetWindowLong(handle, User32.WindowLongIndexFlags.GWL_EXSTYLE));
        iconBitmapSupplier = new Lazy<BitmapSource>(() =>
        {
            try
            {
                var icon = iconSupplier.Value;
                var result = icon != null
                    ? Imaging.CreateBitmapSourceFromHIcon(Icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions())
                    : default;
                result?.Freeze();
                return result;
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to get IconBitmap, class: {Class}", ex);
                return default;
            }
        }, LazyThreadSafetyMode.ExecutionAndPublication);
            
        processDataSupplier = new Lazy<(string processName, string processPath, string commandLine, string processArgs, DateTime createdAt)>(() =>
        {
            try
            {
                string nativeProcessPath;
                try
                {
                    nativeProcessPath = UnsafeNative.QueryFullProcessImageName(ProcessId);
                }
                catch (Exception ex)
                {
                    Log.Debug(() => $"Failed to retrieve native process path for process Id {ProcessId}", ex);
                    nativeProcessPath = default;
                }
                    
                string processPath;
                try
                {
                    var process = Process.GetProcessById(ProcessId);
                    processPath = process.MainModule?.FileName;
                }
                catch (Exception ex)
                {
                    Log.Debug(() => $"Failed to retrieve process times information for process Id {ProcessId}, using native path: {nativeProcessPath}", ex);
                    processPath = nativeProcessPath;
                }
                    
                string processName;
                try
                {
                    if (!string.IsNullOrEmpty(nativeProcessPath))
                    {
                        // always prioritize native path
                        processName = Path.GetFileName(nativeProcessPath);
                    }
                    else
                    {
                        var process = Process.GetProcessById(ProcessId);
                        processName = process.ProcessName;
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug(() => $"Failed to retrieve process name for process Id {ProcessId}", ex);
                    processName = default;
                }

                string commandLine;
                try
                {
                    commandLine = UnsafeNative.GetCommandLine(ProcessId);
                }
                catch (Exception ex)
                {
                    Log.Debug(() => $"Failed to retrieve command linefor process Id {ProcessId}", ex);
                    commandLine = default;
                }

                string processArgs;
                try
                {
                    processArgs = !string.IsNullOrWhiteSpace(commandLine) ? UnsafeNative.CommandLineToArgvW(commandLine).Skip(1).JoinStrings(" ") : default;
                }
                catch (Exception ex)
                {
                    Log.Debug(() => $"Failed to parse command line args for process Id {ProcessId}, command line: {commandLine}", ex);
                    processArgs = default;
                }

                DateTime createdAt = default;
                if (createdAt == default)
                {
                    try
                    {
                        var process = Process.GetProcessById(ProcessId);
                        createdAt = process.StartTime;
                    }
                    catch (Win32Exception ex)
                    {
                        Log.Debug(() => $"Failed to retrieve process times information for process Id {ProcessId}", ex);
                    }
                }
                    
                if (createdAt == default)
                {
                    try
                    {
                        UnsafeNative.GetProcessTimes(ProcessId, out createdAt, out _, out _, out _);
                    }
                    catch (Win32Exception ex)
                    {
                        Log.Debug(() => $"Failed to retrieve native process times information for process Id {ProcessId}", ex);
                    }
                }
                    
                return (processName, processPath, commandLine, processArgs, createdAt);
            }
            catch (Exception ex)
            {
                Log.Debug(() => $"Failed to wrap Process with Id {ProcessId} - {ex.Message} ({ex.GetType().Name})");
            }
            return default;
        });

        parentProcessIdSupplier = new Lazy<int>(() =>
        {
            try
            {
                return UnsafeNative.GetParentProcessId(ProcessId);
            }
            catch (Exception ex)
            {
                Log.Debug($"Failed to get parent process Id of process {ProcessId} - {ex.Message} ({ex.GetType().Name})");
                return default;
            }
        });
    }
        
    private IFluentLog Log { get; }
        
    public IntPtr Handle { get; }

    public string Title { get; }

    public int ParentProcessId => parentProcessIdSupplier.Value;

    public int ProcessId => processIdSupplier.Value.processId;

    public int ThreadId => processIdSupplier.Value.threadId;

    public DateTime CreatedAt => processDataSupplier.Value.createdAt;

    public Rectangle WindowBounds => UnsafeNative.GetWindowRect(Handle);

    public Rectangle ClientBounds => UnsafeNative.GetClientRect(Handle);

    public Rectangle DwmWindowBounds => UnsafeNative.DwmGetWindowFrameBounds(Handle);
        
    public Rectangle ClientRectWithinMonitor => UnsafeNative.GetClientRectWithinMonitor(Handle);

    [JsonIgnore] public Icon Icon => iconSupplier.Value;

    [JsonIgnore] public BitmapSource IconBitmap => iconBitmapSupplier.Value;

    public string Class => classSupplier.Value;

    public string ProcessPath => processDataSupplier.Value.processPath;

    public string ProcessName => processDataSupplier.Value.processName;
        
    public string ProcessArgs => processDataSupplier.Value.processArgs;

    public string CommandLine => processDataSupplier.Value.commandLine;
        
    public int ZOrder { get; set; }

    public User32.WindowStyles WindowStyle => windowStyle.Value;

    public User32.WindowStylesEx WindowStylesEx => windowStyleEx.Value;

    public bool IsVisible => User32.IsWindowVisible(Handle);

    public bool IsIconic => User32.IsIconic(Handle);

    public IWindowHandle Owner => ownerSupplier.Value;
    public IWindowHandle Parent => parentSupplier.Value;

    private static Icon GetWindowIcon(IntPtr handle)
    {
        if (User32.SendMessageTimeout(
                handle,
                User32.WindowMessage.WM_GETICON,
                new IntPtr(0),
                new IntPtr(0),
                User32.SendMessageTimeoutFlags.SMTO_ABORTIFHUNG | User32.SendMessageTimeoutFlags.SMTO_BLOCK,
                500,
                out var hIcon) ==
            IntPtr.Zero)
        {
            hIcon = IntPtr.Zero;
        }

        Icon result = null;
        if (hIcon != IntPtr.Zero)
        {
            result = Icon.FromHandle(hIcon);
        }
        else
        {
            hIcon = UnsafeNative.GetClassLong(handle, UnsafeNative.ClassLong.Icon);

            if (hIcon.ToInt64() != 0)
            {
                result = Icon.FromHandle(hIcon);
            }
        }

        return result;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(Handle.ToHexadecimal());

        if (!string.IsNullOrWhiteSpace(Title))
        {
            sb.Append($" (title: {Title})");
        }

        if (!string.IsNullOrWhiteSpace(Class))
        {
            sb.Append($" (class: {Class})");
        }

        return sb.ToString();
    }

    public bool Equals(IWindowHandle other)
    {
        if (other == null)
        {
            return false;
        }
        return Handle.Equals(other.Handle);
    }

    public override bool Equals(object other)
    {
        if (ReferenceEquals(other, this))
        {
            return true;
        }

        return Equals(other as IWindowHandle);
    }

    public override int GetHashCode()
    {
        return Handle.GetHashCode();
    }

    public void Dispose()
    {
        if (iconSupplier?.IsValueCreated ?? false)
        {
            iconSupplier?.Value?.Dispose();
        }
    }
}