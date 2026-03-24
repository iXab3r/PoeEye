#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Wpf.Automation;

/// <summary>
/// Provides discovery and lookup operations for registered Blazor browser views hosted inside EyeAuras windows.
/// The registry is used by diagnostics, browser automation tooling, and developer helpers that need stable access
/// to live WebView-backed surfaces such as window bodies and title bars.
/// </summary>
public interface IBlazorWindowViewRegistry
{
    /// <summary>
    /// Returns descriptors for all currently registered Blazor browser views.
    /// </summary>
    /// <param name="cancellationToken">
    /// Token used to cancel the enumeration before all descriptors are materialized.
    /// </param>
    /// <returns>
    /// A snapshot of all known browser views ordered by their stable automation identifiers.
    /// </returns>
    Task<IReadOnlyList<BlazorWindowViewDescriptor>> ListViewsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a single registered browser view by its stable automation identifier.
    /// </summary>
    /// <param name="viewAutomationId">
    /// Stable automation identifier of the requested browser view, for example <c>main/body</c>.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the lookup before the descriptor is materialized.
    /// </param>
    /// <returns>
    /// The requested browser view descriptor when found; otherwise <see langword="null"/>.
    /// </returns>
    Task<BlazorWindowViewDescriptor?> GetViewAsync(string viewAutomationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to resolve the live browser view handle for a registered browser view.
    /// </summary>
    /// <param name="viewAutomationId">
    /// Stable automation identifier of the requested browser view.
    /// </param>
    /// <param name="viewHandle">
    /// When this method returns <see langword="true"/>, contains the live handle associated with the requested view.
    /// Otherwise contains an undefined value.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when a matching browser view handle is currently registered; otherwise <see langword="false"/>.
    /// </returns>
    bool TryGetViewHandle(string viewAutomationId, out BlazorWindowViewHandle viewHandle);
}

internal interface IBlazorWindowViewRegistryRegistrar
{
    /// <summary>
    /// Registers a live browser view handle and returns a disposable registration token that removes it from the registry.
    /// </summary>
    /// <param name="viewHandle">
    /// Handle describing the live browser view to register.
    /// </param>
    /// <returns>
    /// A disposable token that unregisters the supplied handle when disposed.
    /// </returns>
    IDisposable Register(BlazorWindowViewHandle viewHandle);
}

/// <summary>
/// Represents a live browser view hosted by a <see cref="BlazorContentControl"/>.
/// The handle exposes stable automation identifiers together with UI-thread-safe operations such as descriptor creation,
/// screenshot capture, and DevTools opening.
/// </summary>
public sealed class BlazorWindowViewHandle
{
    /// <summary>
    /// Initializes a new handle for a live browser view hosted by the specified content control.
    /// </summary>
    /// <param name="contentControl">
    /// Hosting control that owns the live Blazor WebView instance.
    /// </param>
    /// <param name="windowAutomationId">
    /// Stable automation identifier of the owning EyeAuras window.
    /// </param>
    /// <param name="viewRole">
    /// Semantic role of the hosted browser view within the owning window.
    /// </param>
    public BlazorWindowViewHandle(BlazorContentControl contentControl, string windowAutomationId, BlazorWindowViewRole viewRole)
    {
        ContentControl = contentControl ?? throw new ArgumentNullException(nameof(contentControl));
        WindowAutomationId = windowAutomationId?.Trim() ?? string.Empty;
        ViewRole = viewRole;
    }

    /// <summary>
    /// Gets the hosting control that owns the live Blazor WebView instance.
    /// </summary>
    public BlazorContentControl ContentControl { get; }

    /// <summary>
    /// Gets the stable automation identifier of the owning EyeAuras window.
    /// </summary>
    public string WindowAutomationId { get; }

    /// <summary>
    /// Gets the stable automation identifier of the browser view represented by this handle.
    /// </summary>
    public string ViewAutomationId => string.IsNullOrWhiteSpace(WindowAutomationId)
        ? string.Empty
        : $"{WindowAutomationId}/{ViewRole.ToAutomationSegment()}";

    /// <summary>
    /// Gets the semantic role of the hosted browser view within the owning window.
    /// </summary>
    public BlazorWindowViewRole ViewRole { get; }

    /// <summary>
    /// Creates a diagnostic descriptor for the current browser view state on the UI thread.
    /// </summary>
    /// <param name="cancellationToken">
    /// Token used to cancel descriptor creation before the UI-thread work completes.
    /// </param>
    /// <returns>
    /// A snapshot descriptor describing the current state of the browser view.
    /// </returns>
    public async Task<BlazorWindowViewDescriptor> CreateDescriptorAsync(CancellationToken cancellationToken = default)
    {
        return await InvokeOnUiAsync(
                () => new BlazorWindowViewDescriptor(
                    WindowAutomationId: WindowAutomationId,
                    ViewAutomationId: ViewAutomationId,
                    Role: ViewRole.ToAutomationSegment(),
                    IsReady: ContentControl.WebView?.WebView?.CoreWebView2 != null,
                    IsVisible: ContentControl.IsVisible,
                    Title: ContentControl.WebView?.WebView?.CoreWebView2?.DocumentTitle,
                    CurrentUrl: ContentControl.WebView?.WebView?.Source?.ToString(),
                    ViewType: ContentControl.ViewType?.FullName,
                    DataContextType: (ContentControl.Content as IBlazorWindow)?.DataContext?.GetType().FullName,
                    HasUnhandledException: ContentControl.UnhandledException != null),
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Opens Chromium DevTools for the underlying live WebView instance on the UI thread.
    /// </summary>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation before the UI-thread work begins.
    /// </param>
    public async Task OpenDevToolsAsync(CancellationToken cancellationToken = default)
    {
        await InvokeOnUiAsync(async () =>
            {
                await ContentControl.OpenDevTools().ConfigureAwait(true);
                return true;
            }, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Captures a screenshot of the hosted browser view and saves it as a PNG file.
    /// </summary>
    /// <param name="outputDirectory">
    /// Directory where the screenshot file should be written.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation before the screenshot is fully captured and written.
    /// </param>
    /// <returns>
    /// Absolute path to the saved screenshot file.
    /// </returns>
    public async Task<string> TakeScreenshotAsync(string outputDirectory, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException("Output directory must not be empty.", nameof(outputDirectory));
        }

        Directory.CreateDirectory(outputDirectory);
        var bitmap = await InvokeOnUiAsync(
                async () => await ContentControl.WebView.TakeScreenshotAsBitmapSource().ConfigureAwait(true),
                cancellationToken)
            .ConfigureAwait(false);

        var fileName = $"{SanitizePathSegment(ViewAutomationId)}-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}.png";
        var outputPath = Path.Combine(outputDirectory, fileName);
        await SaveBitmapAsync(bitmap, outputPath, cancellationToken).ConfigureAwait(false);
        return outputPath;
    }

    private async Task<T> InvokeOnUiAsync<T>(Func<T> action, CancellationToken cancellationToken)
    {
        if (ContentControl.Dispatcher.CheckAccess())
        {
            cancellationToken.ThrowIfCancellationRequested();
            return action();
        }

        return await ContentControl.Dispatcher
            .InvokeAsync(action, DispatcherPriority.Normal, cancellationToken)
            .Task
            .ConfigureAwait(false);
    }

    private async Task<T> InvokeOnUiAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        if (ContentControl.Dispatcher.CheckAccess())
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await action().ConfigureAwait(true);
        }

        var nestedTask = await ContentControl.Dispatcher
            .InvokeAsync(action, DispatcherPriority.Normal, cancellationToken)
            .Task
            .ConfigureAwait(false);
        return await nestedTask.ConfigureAwait(false);
    }

    private static async Task SaveBitmapAsync(BitmapSource bitmap, string outputPath, CancellationToken cancellationToken)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        await using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        encoder.Save(outputStream);
        await outputStream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string SanitizePathSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "view";
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (invalidChars.Contains(ch) || ch is '/' or '\\' or ':')
            {
                builder.Append('_');
                continue;
            }

            builder.Append(ch);
        }

        return builder.ToString();
    }
}

internal sealed class BlazorWindowViewRegistry : IBlazorWindowViewRegistry, IBlazorWindowViewRegistryRegistrar
{
    private static readonly IFluentLog Log = typeof(BlazorWindowViewRegistry).PrepareLogger();

    private readonly ConcurrentDictionary<string, BlazorWindowViewHandle> viewHandles = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a live browser view handle and returns a token that removes it when disposed.
    /// </summary>
    /// <param name="viewHandle">
    /// Live browser view handle to register.
    /// </param>
    /// <returns>
    /// A disposable token that unregisters the handle when disposed.
    /// </returns>
    public IDisposable Register(BlazorWindowViewHandle viewHandle)
    {
        ArgumentNullException.ThrowIfNull(viewHandle);

        if (string.IsNullOrWhiteSpace(viewHandle.ViewAutomationId))
        {
            return Disposable.Empty;
        }

        viewHandles[viewHandle.ViewAutomationId] = viewHandle;
        Log.Debug($"Registered Blazor window view: {viewHandle.ViewAutomationId}");
        return Disposable.Create(() =>
        {
            if (viewHandles.TryGetValue(viewHandle.ViewAutomationId, out var existing) && ReferenceEquals(existing, viewHandle))
            {
                viewHandles.TryRemove(viewHandle.ViewAutomationId, out _);
                Log.Debug($"Unregistered Blazor window view: {viewHandle.ViewAutomationId}");
            }
        });
    }

    /// <summary>
    /// Returns descriptors for all currently registered browser views.
    /// </summary>
    /// <param name="cancellationToken">
    /// Token used to cancel descriptor materialization before completion.
    /// </param>
    /// <returns>
    /// A snapshot of all known browser views ordered by view automation identifier.
    /// </returns>
    public async Task<IReadOnlyList<BlazorWindowViewDescriptor>> ListViewsAsync(CancellationToken cancellationToken = default)
    {
        var handles = viewHandles.Values
            .OrderBy(x => x.ViewAutomationId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var descriptors = new List<BlazorWindowViewDescriptor>(handles.Length);
        foreach (var handle in handles)
        {
            descriptors.Add(await handle.CreateDescriptorAsync(cancellationToken).ConfigureAwait(false));
        }

        return descriptors;
    }

    /// <summary>
    /// Resolves a single browser view descriptor by stable automation identifier.
    /// </summary>
    /// <param name="viewAutomationId">
    /// Stable automation identifier of the requested browser view.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the lookup before the descriptor is materialized.
    /// </param>
    /// <returns>
    /// The requested descriptor when found; otherwise <see langword="null"/>.
    /// </returns>
    public async Task<BlazorWindowViewDescriptor?> GetViewAsync(string viewAutomationId, CancellationToken cancellationToken = default)
    {
        if (!TryGetViewHandle(viewAutomationId, out var viewHandle))
        {
            return null;
        }

        return await viewHandle.CreateDescriptorAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Attempts to resolve the live browser view handle for the specified stable automation identifier.
    /// </summary>
    /// <param name="viewAutomationId">
    /// Stable automation identifier of the requested browser view.
    /// </param>
    /// <param name="viewHandle">
    /// When this method returns <see langword="true"/>, contains the resolved live browser view handle.
    /// Otherwise contains an undefined value.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when a matching browser view handle is currently registered; otherwise <see langword="false"/>.
    /// </returns>
    public bool TryGetViewHandle(string viewAutomationId, out BlazorWindowViewHandle viewHandle)
    {
        viewHandle = null!;
        if (string.IsNullOrWhiteSpace(viewAutomationId))
        {
            return false;
        }

        return viewHandles.TryGetValue(viewAutomationId.Trim(), out viewHandle);
    }
}
