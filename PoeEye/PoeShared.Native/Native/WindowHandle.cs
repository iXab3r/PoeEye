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
using log4net;
using Newtonsoft.Json;
using PInvoke;
using PoeShared.Logging;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using Win32Exception = System.ComponentModel.Win32Exception;
// ReSharper disable CA1416

namespace PoeShared.Native
{
    internal sealed class WindowHandle : IWindowHandle
    {
        private static readonly IFluentLog Log = typeof(WindowHandle).PrepareLogger();

        private readonly Lazy<string> classSupplier;
        private readonly Lazy<Rectangle> windowBoundsSupplier;
        private readonly Lazy<Rectangle> dwmWindowBoundsSupplier;
        private readonly Lazy<Rectangle> clientBoundsSupplier;
        private readonly Lazy<Icon> iconSupplier;
        private readonly Lazy<BitmapSource> iconBitmapSupplier;
        private readonly Lazy<(string processName, string processPath, string commandLine, string processArgs, DateTime createdAt)> processDataSupplier;
        
        public WindowHandle(IntPtr handle)
        {
            Handle = handle;
            Title = UnsafeNative.GetWindowTitle(handle);
            ProcessId = UnsafeNative.GetProcessIdByWindowHandle(handle);
            
            classSupplier = new Lazy<string>(() => UnsafeNative.GetWindowClass(handle), LazyThreadSafetyMode.ExecutionAndPublication);
            windowBoundsSupplier = new Lazy<Rectangle>(() => UnsafeNative.GetWindowRect(handle), LazyThreadSafetyMode.ExecutionAndPublication);
            dwmWindowBoundsSupplier = new Lazy<Rectangle>(() => UnsafeNative.DwmGetWindowFrameBounds(handle), LazyThreadSafetyMode.ExecutionAndPublication);
            clientBoundsSupplier = new Lazy<Rectangle>(() => UnsafeNative.GetClientRect(handle), LazyThreadSafetyMode.ExecutionAndPublication);
            iconSupplier = new Lazy<Icon>(() => GetWindowIcon(handle), LazyThreadSafetyMode.ExecutionAndPublication);
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
                    Log.Warn($"Failed to get IconBitmap, window: {Title}, class: {Class}", ex);
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
                        Log.Debug($"Failed to retrieve native process path for process Id {ProcessId}, window: {Title}, class: {Class}", ex);
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
                        Log.Debug($"Failed to retrieve process times information for process Id {ProcessId}, window: {Title}, class: {Class}, using native path: {nativeProcessPath}", ex);
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
                        Log.Debug($"Failed to retrieve process name for process Id {ProcessId}, window: {Title}, class: {Class}", ex);
                        processName = default;
                    }

                    string commandLine;
                    try
                    {
                        commandLine = UnsafeNative.GetCommandLine(ProcessId);
                    }
                    catch (Exception ex)
                    {
                        Log.Debug($"Failed to retrieve command linefor process Id {ProcessId}, window: {Title}, class: {Class}", ex);
                        commandLine = default;
                    }

                    string processArgs;
                    try
                    {
                        processArgs = !string.IsNullOrWhiteSpace(commandLine) ? UnsafeNative.CommandLineToArgvW(commandLine).Skip(1).JoinStrings(" ") : default;
                    }
                    catch (Exception ex)
                    {
                        Log.Debug($"Failed to parse command line args for process Id {ProcessId}, window: {Title}, class: {Class}, command line: {commandLine}", ex);
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
                            Log.Debug($"Failed to retrieve process times information for process Id {ProcessId}, window: {Title}, class: {Class}", ex);
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
                            Log.Debug($"Failed to retrieve native process times information for process Id {ProcessId}, window: {Title}, class: {Class}", ex);
                        }
                    }
                    
                    return (processName, processPath, commandLine, processArgs, createdAt);
                }
                catch (Exception ex)
                {
                    Log.Debug($"Failed to wrap Process with Id {ProcessId}, window: {Title}, class: {Class} - {ex.Message} ({ex.GetType().Name})");
                }
                return default;
            });
        }
        
        public IntPtr Handle { get; }

        public string Title { get; }

        public int ProcessId { get; }

        public DateTime CreatedAt => processDataSupplier.Value.createdAt;

        public Rectangle WindowBounds => windowBoundsSupplier.Value;

        public Rectangle ClientBounds => clientBoundsSupplier.Value;

        public Rectangle DwmWindowBounds => dwmWindowBoundsSupplier.Value;

        [JsonIgnore] public Icon Icon => iconSupplier.Value;

        [JsonIgnore] public BitmapSource IconBitmap => iconBitmapSupplier.Value;

        public string Class => classSupplier.Value;

        public string ProcessPath => processDataSupplier.Value.processPath;

        public string ProcessName => processDataSupplier.Value.processName;
        
        public string ProcessArgs => processDataSupplier.Value.processArgs;

        public string CommandLine => processDataSupplier.Value.commandLine;
        
        public int ZOrder { get; set; }

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
}