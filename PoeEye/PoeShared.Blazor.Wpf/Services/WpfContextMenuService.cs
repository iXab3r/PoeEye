#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AntDesign;
using AntDesign.JsInterop;
using Microsoft.AspNetCore.Components;
using PInvoke;
using PoeShared.Blazor.Controls.Scaffolding;
using PoeShared.Blazor.Services;
using PoeShared.Blazor.Wpf.Scaffolding;
using PoeShared.Logging;
using PoeShared.Native;
using PoeShared.Scaffolding;
using Image = System.Windows.Controls.Image;
using MenuItem = System.Windows.Controls.MenuItem;

namespace PoeShared.Blazor.Wpf.Services;

internal sealed class WpfContextMenuService : IBlazorContextMenuService
{
    private static readonly IFluentLog Log = typeof(WpfContextMenuService).PrepareLogger();

    private readonly INotificationService notificationService;
    private readonly ConcurrentDictionary<string, ContextMenuManager> managersById = new();
    private readonly Subject<IList<BlazorContextMenuItem>> itemsSink = new();
    private readonly DispatcherScheduler scheduler;

    public WpfContextMenuService(
        IDomEventListener domEventListener,
        INotificationService notificationService)
    {
        DomEventListener = domEventListener;
        this.notificationService = notificationService;
        scheduler = DispatcherScheduler.Current;
    }

    public IObservable<IList<BlazorContextMenuItem>> WhenContextMenuRequested => itemsSink.AsObservable();

    public IDomEventListener DomEventListener { get; }

    public async Task<IDisposable> RegisterAsync(ElementReference elementRef, Action<IList<BlazorContextMenuItem>> handler)
    {
        var cmId = $"CM-WPF-{Guid.NewGuid()}";
        try
        {
            var manager = new ContextMenuManager(elementRef, cmId, handler);
            managersById[cmId] = manager;
            DomEventListener.AddExclusive<JsonElement>(
                elementRef,
                "contextmenu",
                json => HandleContextMenuJson(json, manager),
                stopPropagation: true,
                preventDefault: true);
        }
        catch (Exception e)
        {
            Log.Warn($"Failed to register context menu for {elementRef}", e);
            return Disposable.Empty;
        }

        return Disposable.Create(() =>
        {
            try
            {
                managersById.TryRemove(cmId, out _);
                DomEventListener.RemoveExclusive(elementRef, "contextmenu");
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to remove context menu from {elementRef}", e);
            }
        });
    }

    public async Task ShowContextMenu(IList<BlazorContextMenuItem> items)
    {
        var cursorPosition = System.Windows.Forms.Cursor.Position;
        var diPosition = ConvertToDeviceIndependentPixels(cursorPosition.X, cursorPosition.Y);
        ShowMenuCore(items, new BlazorContextMenuInvokeContext()
        {
            X = (int) diPosition.X,
            Y = (int) diPosition.Y
        });
    }

    private async void HandleContextMenuJson(JsonElement obj, ContextMenuManager manager)
    {
        try
        {
            var json = obj.ToString();
            var jsonBack = JsonSerializer.Deserialize<JsonElement>(json);
            var mouse = MouseEventArgsJsonReader.Read(jsonBack);

            var items = new List<BlazorContextMenuItem>();
            manager.Handler(items);
            itemsSink.OnNext(items);

            if (items.Count <= 0)
            {
                return;
            }

            var invokeCtx = new BlazorContextMenuInvokeContext
            {
                ComponentId = manager.DataCmId,
                X = (int) Math.Round(mouse.ScreenX),
                Y = (int) Math.Round(mouse.ScreenY)
            };

            await ShowWpfContextMenuAsync(items, invokeCtx);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to show WPF context menu", ex);
            notificationService.Error(new NotificationConfig()
            {
                Message = $"Failed to show WPF context menu",
                NotificationType = NotificationType.Error,
                Duration = 30,
                Placement = NotificationPlacement.TopRight
            }).AndForget(ignoreExceptions: true);
        }
    }

    private async Task ShowWpfContextMenuAsync(IList<BlazorContextMenuItem> items, BlazorContextMenuInvokeContext ctx)
    {
        // Ensure on UI thread
        if (!scheduler.CheckAccess())
        {
            await scheduler.Dispatcher.InvokeAsync(() => ShowMenuCore(items, ctx));
        }
        else
        {
            ShowMenuCore(items, ctx);
        }
    }

    private void ShowMenuCore(IList<BlazorContextMenuItem> items, BlazorContextMenuInvokeContext ctx)
    {
        var menu = new ContextMenu
        {
            Placement = PlacementMode.AbsolutePoint,
            HorizontalOffset = ctx.X,
            VerticalOffset = ctx.Y,
            StaysOpen = false,
        };

        Populate(menu.Items, items.ToArray(), ctx, notificationService);

        // Open the menu
        menu.IsOpen = true;
    }

    private static void Populate(ItemCollection target, ReadOnlySpan<BlazorContextMenuItem> items, BlazorContextMenuInvokeContext ctx,
        INotificationService notificationService)
    {
        for (var i = 0; i < items.Length; i++)
        {
            var item = items[i];
            switch (item)
            {
                case BlazorContextMenuSeparator:
                    target.Add(new Separator());
                    break;
                case BlazorContextMenuCommand cmd:
                {
                    var mi = new MenuItem
                    {
                        Header = cmd.Label,
                        IsEnabled = cmd.Enabled
                    };

                    if (cmd.IconFactory != null)
                    {
                        var iconObject = cmd.IconFactory();
                        if (iconObject is Stream stream)
                        {
                            using var iconStream = stream;
                            var image = CreateImage(iconStream);
                            mi.Icon = image;
                        }
                        else if (iconObject is string iconText)
                        {
                            mi.Icon = iconText;
                        }
                    }

                    Wire(mi, cmd.OnInvokeAsync, ctx, notificationService);
                    target.Add(mi);
                    break;
                }
                case BlazorContextMenuCheckBox cb:
                {
                    var mi = new MenuItem
                    {
                        Header = cb.Label,
                        IsEnabled = cb.Enabled,
                        IsCheckable = true,
                        IsChecked = cb.IsChecked
                    };
                    Wire(mi, cb.OnToggleAsync, ctx, notificationService);
                    target.Add(mi);
                    break;
                }
                case BlazorContextMenuRadio r:
                {
                    var mi = new MenuItem
                    {
                        Header = r.Label,
                        IsEnabled = r.Enabled,
                        IsCheckable = true,
                        IsChecked = r.IsChecked
                    };
                    Wire(mi, r.OnSelectAsync, ctx, notificationService);
                    target.Add(mi);
                    break;
                }
                case CmSubmenu sm:
                {
                    var mi = new MenuItem {Header = sm.Label, IsEnabled = sm.Enabled};
                    Populate(mi.Items, sm.Children.AsSpan(), ctx, notificationService);
                    target.Add(mi);
                    break;
                }
            }
        }
    }

    private static Image CreateImage(Stream stream)
    {
        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.CacheOption = BitmapCacheOption.OnLoad; // so stream can close
        bmp.StreamSource = stream;
        bmp.EndInit();
        bmp.Freeze();
        return new Image {Source = bmp, Width = bmp.Width, Height = bmp.Height, Stretch = Stretch.None};
    }

    private static void Wire(
        MenuItem menuItem,
        Func<BlazorContextMenuInvokeContext, Task>? handler,
        BlazorContextMenuInvokeContext ctx,
        INotificationService notificationService)
    {
        if (handler == null)
        {
            return;
        }

        menuItem.Click += async (_, _) =>
        {
            try
            {
                await handler(ctx).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error($"Exception in WPF menu item '{menuItem.Header}'", ex);
                notificationService.Error(new NotificationConfig()
                {
                    Message = $"Operation failed, menu item '{menuItem.Header}'",
                    NotificationType = NotificationType.Error,
                    Duration = 30,
                    Placement = NotificationPlacement.TopRight
                }).AndForget(ignoreExceptions: true);
            }
        };
    }

    private static Vector2 ConvertToDeviceIndependentPixels(int deviceX, int deviceY)
    {
        // ctx.X/Y are screen coordinates in device pixels; convert to device-independent pixels (DIPs) via WinAPI
        float dipX = deviceX;
        float dipY = deviceY;
        try
        {
            // Default scale factors
            float scaleX = 1f, scaleY = 1f;

            // Prefer per-monitor DPI using SHCore when available
            try
            {
                var pt = new POINT {x = deviceX, y = deviceY};
                var monitor = User32.MonitorFromPoint(pt, User32.MonitorOptions.MONITOR_DEFAULTTONEAREST);
                if (monitor != IntPtr.Zero)
                {
                    var hr = SHCore.GetDpiForMonitor(monitor, MONITOR_DPI_TYPE.MDT_DEFAULT, out var dpiX, out var dpiY);
                    if (hr.Succeeded && dpiX > 0 && dpiY > 0)
                    {
                        scaleX = 96f / dpiX;
                        scaleY = 96f / dpiY;
                    }
                }
            }
            catch
            {
                // ignore and fallback below
            }

            // Fallback to window DPI (Windows 10+ API) if per-monitor DPI not obtained
            if (Math.Abs(scaleX - 1d) < double.Epsilon && Math.Abs(scaleY - 1d) < double.Epsilon)
            {
                try
                {
                    IntPtr hwnd = IntPtr.Zero;
                    var window = Application.Current?.MainWindow;
                    if (window != null)
                    {
                        hwnd = new System.Windows.Interop.WindowInteropHelper(window).EnsureHandle();
                    }

                    if (hwnd == IntPtr.Zero)
                    {
                        hwnd = UnsafeNative.GetForegroundWindow();
                    }

                    if (hwnd != IntPtr.Zero)
                    {
                        var windowDpi = User32.GetDpiForWindow(hwnd);
                        if (windowDpi > 0)
                        {
                            var s = 96f / windowDpi;
                            scaleX = s;
                            scaleY = s;
                        }
                    }
                }
                catch
                {
                    // ignore and keep scale=1
                }
            }

            dipX = deviceX * scaleX;
            dipY = deviceY * scaleY;
        }
        catch
        {
            // ignore and use raw values
        }

        return new Vector2(dipX, dipY);
    }

    private readonly record struct ContextMenuManager(ElementReference ElementRef, string DataCmId, Action<IList<BlazorContextMenuItem>> Handler);
}