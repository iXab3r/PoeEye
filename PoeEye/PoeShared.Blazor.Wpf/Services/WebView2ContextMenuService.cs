using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Web.WebView2.Core;
using PoeShared.Blazor.Services;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Wpf.Services;

internal sealed class WebView2ContextMenuService : IBlazorContextMenuService
{
    private static readonly IFluentLog Log = typeof(WebView2ContextMenuService).PrepareLogger();
    private static readonly string DataCmId = "data-cm-id";

    private readonly ICoreWebView2Accessor coreWebView2Accessor;
    private readonly IJsPoeBlazorUtils poeBlazorUtils;
    private readonly ConcurrentDictionary<string, ContextMenuManager> managersById = new();
    private readonly Subject<IList<CmItem>> itemsSink = new();
    
    public WebView2ContextMenuService(
        ICoreWebView2Accessor coreWebView2Accessor,
        IJsPoeBlazorUtils poeBlazorUtils)
    {
        this.coreWebView2Accessor = coreWebView2Accessor;
        this.poeBlazorUtils = poeBlazorUtils;

        coreWebView2Accessor.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
        coreWebView2Accessor.CoreWebView2.ContextMenuRequested += CoreWebView2OnContextMenuRequested;
    }

    public IObservable<IList<CmItem>> WhenContextMenuRequested => itemsSink.AsObservable();
    
    public async Task<IDisposable> RegisterAsync(ElementReference elementRef, Action<IList<CmItem>> handler)
    {
        var cmId = $"CM-{Guid.NewGuid()}";
        // Tag the element with a unique context menu id so we can resolve it from JS hit-testing
        await poeBlazorUtils.SetAttribute(elementRef, DataCmId, cmId);
        managersById[cmId] = new ContextMenuManager(elementRef, cmId, handler);
        
        return Disposable.Create(() =>
        {
            managersById.TryRemove(cmId, out _);
            poeBlazorUtils.RemoveAttribute(elementRef, DataCmId).AndForget(ignoreExceptions: true);
        });
    }

    private async void CoreWebView2OnContextMenuRequested(object sender, CoreWebView2ContextMenuRequestedEventArgs e)
    {
        CoreWebView2Deferral deferral = null;
        try
        {
            deferral = e.GetDeferral();

            var webView = (CoreWebView2) sender;
            e.MenuItems.Clear(); //remove default items

            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(5000)); // keep this tight
            var x = e.Location.X;
            var y = e.Location.Y;

            // Example: ask JS for breadcrumbs (or just the nearest data-cm-id).
            // This should be a *small* script call that returns a tiny JSON payload.
            var crumbs = await poeBlazorUtils.GetElementBreadcrumbsAt(x, y, 128, new[] {DataCmId}, cts.Token);

            var contextMenuElement = crumbs?.FirstOrDefault(n =>
                n.Node?.Attributes != null &&
                n.Node.Attributes.TryGetValue(DataCmId, out var v) &&
                !string.IsNullOrWhiteSpace(v));

            if (contextMenuElement == null)
            {
                e.Handled = true;
                return;
            }
            
            var items = new List<CmItem>();
            if (managersById.TryGetValue(contextMenuElement.Node.Attributes[DataCmId], out var manager))
            {
                manager.Handler(items);
            }
            
            itemsSink.OnNext(items);
            
            Populate(Log, webView, e.MenuItems, items.ToArray(), new CmInvokeContext()
            {
                ComponentId = contextMenuElement.Node.Attributes[DataCmId],
                Tag = contextMenuElement,
                X = e.Location.X,
                Y = e.Location.Y,
            });
            e.Handled = false;
        }
        catch (OperationCanceledException)
        {
            // Timed out — safest fallback is to leave the default menu
            e.Handled = false;
        }
        catch (Exception ex)
        {
            Log.Error("Failed to show context menu", ex);
            e.Handled = false;
        }
        finally
        {
            deferral?.Complete(); // MUST happen even on exceptions/timeouts
        }
    }

    public static void Populate(
        IFluentLog log,
        CoreWebView2 webview,
        IList<CoreWebView2ContextMenuItem> targetCollection,
        ReadOnlySpan<CmItem> items,
        CmInvokeContext invokeContext)
    {
        for (var i = 0; i < items.Length; i++)
        {
            var item = items[i];

            switch (item)
            {
                case CmSeparator:
                {
                    var sep = webview.Environment.CreateContextMenuItem(string.Empty, Stream.Null, CoreWebView2ContextMenuItemKind.Separator);
                    targetCollection.Add(sep);
                    break;
                }

                case CmCommand cmd:
                {
                    using var icon = cmd.IconFactory?.Invoke() ?? Stream.Null; // can be null
                    var native = webview.Environment.CreateContextMenuItem(cmd.Label, icon, CoreWebView2ContextMenuItemKind.Command);
                    native.IsEnabled = cmd.Enabled;

                    Wire(native, cmd.OnInvokeAsync);
                    targetCollection.Add(native);
                    break;
                }

                case CmCheckBox cb:
                {
                    var native = webview.Environment.CreateContextMenuItem(cb.Label, Stream.Null, CoreWebView2ContextMenuItemKind.CheckBox);
                    native.IsEnabled = cb.Enabled;
                    native.IsChecked = cb.IsChecked;

                    Wire(native, cb.OnToggleAsync);
                    targetCollection.Add(native);
                    break;
                }

                case CmRadio r:
                {
                    var native = webview.Environment.CreateContextMenuItem(
                        r.Label, Stream.Null, CoreWebView2ContextMenuItemKind.Radio);
                    native.IsEnabled = r.Enabled;
                    native.IsChecked = r.IsChecked;

                    Wire(native, r.OnSelectAsync);
                    targetCollection.Add(native);
                    break;
                }

                case CmSubmenu sm:
                {
                    var native = webview.Environment.CreateContextMenuItem(
                        sm.Label, Stream.Null, CoreWebView2ContextMenuItemKind.Submenu);
                    native.IsEnabled = sm.Enabled;

                    // Recurse into submenu children
                    Populate(log, webview, native.Children, sm.Children.AsSpan(), invokeContext);
                    targetCollection.Add(native);
                    break;
                }
            }
        }

        return;

        // One shared handler to avoid capturing per item
        void Wire(CoreWebView2ContextMenuItem native, Func<CmInvokeContext, Task>? handler)
        {
            if (handler is null)
            {
                return;
            }

            native.CustomItemSelected += async (_, __) =>
            {
                try
                {
                    await handler(invokeContext).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.Error($"Exception in custom menu item {native.Name} {native.Label}", ex);
                }
            };
        }
    }

    private readonly record struct ContextMenuManager(ElementReference ElementRef, string DataCmId, Action<IList<CmItem>> Handler);
}