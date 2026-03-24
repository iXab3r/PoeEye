using System;
using System.Threading;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Wpf.Automation;

/// <summary>
/// Describes the process-wide WebView2 environment settings that affect how hosted browser instances are created.
/// These settings are effectively locked after the first WebView2 environment is initialized in the current process.
/// </summary>
/// <param name="BrowserExecutableFolder">
/// Optional path to a custom browser executable folder that should be used for WebView2 initialization.
/// When <see langword="null"/> or empty, the default WebView2 runtime resolution is used.
/// </param>
/// <param name="AdditionalBrowserArguments">
/// Additional command-line arguments that should be passed to the browser process when the environment is created.
/// </param>
public sealed record WebView2EnvironmentSpec(
    string? BrowserExecutableFolder = null,
    string AdditionalBrowserArguments = "")
{
    /// <summary>
    /// Gets an empty environment specification that uses the default browser runtime resolution and no extra arguments.
    /// </summary>
    public static WebView2EnvironmentSpec Empty { get; } = new();
}

/// <summary>
/// Describes the result of applying a requested WebView2 environment specification.
/// The resolution reports the startup specification, the requested specification, the effective specification that remains
/// active for the process, and whether an application restart is required for the requested changes to take effect.
/// </summary>
/// <param name="StartupSpec">
/// Environment specification that was active when WebView2 was first locked for the current process.
/// </param>
/// <param name="RequestedSpec">
/// Normalized environment specification that the caller attempted to apply.
/// </param>
/// <param name="EffectiveSpec">
/// Environment specification that remains effective for the current process after applying the request.
/// </param>
/// <param name="RestartRequired">
/// Indicates whether the requested changes differ from the locked process-wide WebView2 environment and therefore
/// require an application restart to take effect.
/// </param>
/// <param name="Message">
/// Human-readable explanation of the resolution result, suitable for logging or diagnostics.
/// </param>
public sealed record WebView2EnvironmentResolution(
    WebView2EnvironmentSpec StartupSpec,
    WebView2EnvironmentSpec RequestedSpec,
    WebView2EnvironmentSpec EffectiveSpec,
    bool RestartRequired,
    string Message);

/// <summary>
/// Applies requested WebView2 environment settings and reports whether those settings can take effect immediately
/// or require an application restart because the process-wide environment is already locked.
/// </summary>
public interface IWebView2EnvironmentController
{
    /// <summary>
    /// Applies the requested WebView2 environment specification for the current process.
    /// </summary>
    /// <param name="requestedSpec">
    /// Desired environment specification to apply.
    /// </param>
    /// <returns>
    /// Resolution information describing the effective process-wide WebView2 environment after the request is processed.
    /// </returns>
    WebView2EnvironmentResolution ApplyRequestedSpec(WebView2EnvironmentSpec requestedSpec);
}

internal sealed class WebView2EnvironmentController : IWebView2EnvironmentController
{
    private static readonly IFluentLog Log = typeof(WebView2EnvironmentController).PrepareLogger();

    private WebView2EnvironmentSpec? lockedSpec;
    private string lastLoggedMismatchMessage = string.Empty;

    /// <summary>
    /// Applies the requested WebView2 environment specification while honoring the process-wide environment lock.
    /// The first successfully applied specification becomes the effective startup specification for the process.
    /// Later conflicting requests are reported back as restart-required resolutions.
    /// </summary>
    /// <param name="requestedSpec">
    /// Desired WebView2 environment specification to apply.
    /// </param>
    /// <returns>
    /// Resolution information describing the active environment and whether the requested changes require restart.
    /// </returns>
    public WebView2EnvironmentResolution ApplyRequestedSpec(WebView2EnvironmentSpec requestedSpec)
    {
        var normalizedRequestedSpec = Normalize(requestedSpec);
        var previousSpec = Interlocked.CompareExchange(ref lockedSpec, normalizedRequestedSpec, null);
        var effectiveSpec = previousSpec ?? normalizedRequestedSpec;

        if (Equals(normalizedRequestedSpec, effectiveSpec))
        {
            return new WebView2EnvironmentResolution(
                StartupSpec: effectiveSpec,
                RequestedSpec: normalizedRequestedSpec,
                EffectiveSpec: effectiveSpec,
                RestartRequired: false,
                Message: previousSpec == null
                    ? $"Locked WebView2 environment spec: {FormatForLog(effectiveSpec)}"
                    : $"Reusing locked WebView2 environment spec: {FormatForLog(effectiveSpec)}");
        }

        var message =
            $"Requested WebView2 environment spec differs from the locked process-wide spec. " +
            $"Startup={FormatForLog(effectiveSpec)}, Effective={FormatForLog(effectiveSpec)}, Requested={FormatForLog(normalizedRequestedSpec)}. " +
            "Restart the app to apply changes.";

        var previousLoggedMessage = Interlocked.Exchange(ref lastLoggedMismatchMessage, message);
        if (!string.Equals(previousLoggedMessage, message, StringComparison.Ordinal))
        {
            Log.Warn(message);
        }

        return new WebView2EnvironmentResolution(
            StartupSpec: effectiveSpec,
            RequestedSpec: normalizedRequestedSpec,
            EffectiveSpec: effectiveSpec,
            RestartRequired: true,
            Message: message);
    }

    private static WebView2EnvironmentSpec Normalize(WebView2EnvironmentSpec spec)
    {
        return new WebView2EnvironmentSpec(
            BrowserExecutableFolder: string.IsNullOrWhiteSpace(spec.BrowserExecutableFolder) ? null : spec.BrowserExecutableFolder.Trim(),
            AdditionalBrowserArguments: string.IsNullOrWhiteSpace(spec.AdditionalBrowserArguments) ? string.Empty : spec.AdditionalBrowserArguments.Trim());
    }

    private static string FormatForLog(WebView2EnvironmentSpec spec)
    {
        return $"{{ BrowserExecutableFolder = {spec.BrowserExecutableFolder ?? "<null>"}, AdditionalBrowserArguments = {spec.AdditionalBrowserArguments ?? string.Empty} }}";
    }
}
