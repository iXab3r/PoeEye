using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace PoeShared.Scaffolding;

/// <summary>
/// Provides helpers for discovering local network addresses that are useful for local inter-process communication.
/// </summary>
public static class NetworkUtils
{
    /// <summary>
    /// Host alias that should be expanded to the Windows-side WSL host addresses discovered at runtime.
    /// </summary>
    public const string WslHostAlias = "localhost-wsl";

    /// <summary>
    /// Describes how WSL host aliases were expanded into concrete URLs.
    /// </summary>
    /// <param name="ConfiguredUrls">Normalized input URLs before alias expansion.</param>
    /// <param name="ExpandedUrls">Normalized URLs after alias expansion has been applied.</param>
    /// <param name="WslHostAddresses">Unique WSL-reachable Windows host IPv4 addresses used during expansion.</param>
    /// <param name="ExpandedAliasUrls">Configured URLs that used the WSL alias and were expanded successfully.</param>
    /// <param name="SkippedAliasUrls">Configured URLs that used the WSL alias but could not be expanded because no suitable WSL host addresses were available.</param>
    public readonly record struct WslHostUrlExpansionResult(
        IReadOnlyList<string> ConfiguredUrls,
        IReadOnlyList<string> ExpandedUrls,
        IReadOnlyList<IPAddress> WslHostAddresses,
        IReadOnlyList<string> ExpandedAliasUrls,
        IReadOnlyList<string> SkippedAliasUrls)
    {
        /// <summary>
        /// Gets a value indicating whether the configured URLs contained at least one WSL host alias.
        /// </summary>
        public bool HasWslHostAlias => ExpandedAliasUrls.Count > 0 || SkippedAliasUrls.Count > 0;
    }

    private static bool IsWindows()
    {
#if NET5_0_OR_GREATER
        return OperatingSystem.IsWindows();
#else
        return System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
#endif
    }

    /// <summary>
    /// Gets IPv4 addresses assigned to active Windows network interfaces that represent the Windows host side of a WSL virtual network.
    /// </summary>
    /// <returns>
    /// An ordered list of unique non-loopback IPv4 addresses that can be used by WSL clients to reach services hosted on Windows.
    /// Returns an empty list when the current process is not running on Windows or when no active WSL host interfaces are available.
    /// </returns>
    public static IReadOnlyList<IPAddress> GetWslHostAddresses()
    {
        if (!IsWindows())
        {
            return Array.Empty<IPAddress>();
        }

        var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Select(ToNetworkInterfaceInfo)
            .ToArray();
        return GetWslHostAddresses(networkInterfaces);
    }

    /// <summary>
    /// Expands URLs that use the <see cref="WslHostAlias"/> host into concrete WSL-reachable Windows host IP addresses discovered at runtime.
    /// </summary>
    /// <param name="urls">URLs to normalize and expand.</param>
    /// <returns>
    /// A result object containing the normalized input URLs, concrete expanded URLs, discovered WSL host addresses,
    /// and any alias URLs that had to be skipped because no WSL host addresses were available.
    /// </returns>
    public static WslHostUrlExpansionResult ExpandWslHostAliases(IEnumerable<string>? urls)
    {
        return ExpandWslHostAliases(urls, GetWslHostAddresses());
    }

    /// <summary>
    /// Expands URLs that use the <see cref="WslHostAlias"/> host into the supplied concrete Windows host IP addresses.
    /// </summary>
    /// <param name="urls">URLs to normalize and expand.</param>
    /// <param name="wslHostAddresses">Concrete WSL-reachable Windows host IPv4 addresses to use for alias expansion.</param>
    /// <returns>
    /// A result object containing the normalized input URLs, concrete expanded URLs, supplied WSL host addresses,
    /// and any alias URLs that had to be skipped because no usable addresses were available.
    /// </returns>
    public static WslHostUrlExpansionResult ExpandWslHostAliases(IEnumerable<string>? urls, IEnumerable<IPAddress>? wslHostAddresses)
    {
        var configuredUrls = NormalizeUrls(urls);
        var hostAddresses = NormalizeWslHostAddresses(wslHostAddresses);
        var expandedUrls = new List<string>(configuredUrls.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var expandedAliasUrls = new List<string>();
        var skippedAliasUrls = new List<string>();

        foreach (var configuredUrl in configuredUrls)
        {
            var uri = TryParseAbsoluteUri(configuredUrl);
            if (uri == null || !IsWslHostAlias(uri.Host))
            {
                AddExpandedUrl(expandedUrls, seen, configuredUrl);
                continue;
            }

            if (hostAddresses.Count == 0)
            {
                skippedAliasUrls.Add(configuredUrl);
                continue;
            }

            expandedAliasUrls.Add(configuredUrl);
            foreach (var hostAddress in hostAddresses)
            {
                var builder = new UriBuilder(uri)
                {
                    Host = hostAddress.ToReadableString()
                };
                AddExpandedUrl(expandedUrls, seen, builder.Uri.AbsoluteUri);
            }
        }

        return new WslHostUrlExpansionResult(
            configuredUrls,
            expandedUrls,
            hostAddresses,
            expandedAliasUrls,
            skippedAliasUrls);
    }

    /// <summary>
    /// Determines whether the supplied host name is the special alias used for WSL host expansion.
    /// </summary>
    /// <param name="host">Host name to inspect.</param>
    /// <returns><see langword="true"/> when the host matches <see cref="WslHostAlias"/>; otherwise, <see langword="false"/>.</returns>
    public static bool IsWslHostAlias(string host)
    {
        return string.Equals(host?.Trim(), WslHostAlias, StringComparison.OrdinalIgnoreCase);
    }

    internal static IReadOnlyList<IPAddress> GetWslHostAddresses(IEnumerable<NetworkInterfaceInfo> networkInterfaces)
    {
        var result = new List<IPAddress>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var networkInterface in networkInterfaces.EmptyIfNull())
        {
            if (!IsWslNetworkInterface(networkInterface))
            {
                continue;
            }

            if (networkInterface.OperationalStatus != OperationalStatus.Up)
            {
                continue;
            }

            foreach (var address in networkInterface.UnicastAddresses.EmptyIfNull())
            {
                if (!IsUsableHostAddress(address))
                {
                    continue;
                }

                if (seen.Add(address.ToString()))
                {
                    result.Add(address);
                }
            }
        }

        return result;
    }

    internal static bool IsWslNetworkInterface(NetworkInterfaceInfo networkInterface)
    {
        var name = networkInterface.Name ?? string.Empty;
        var description = networkInterface.Description ?? string.Empty;
        return name.Contains("WSL", StringComparison.OrdinalIgnoreCase) ||
               description.Contains("WSL", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsUsableHostAddress(IPAddress address)
    {
        return address.AddressFamily == AddressFamily.InterNetwork &&
               !IPAddress.IsLoopback(address) &&
               !address.Equals(IPAddress.Any);
    }

    internal readonly record struct NetworkInterfaceInfo(
        string Name,
        string Description,
        OperationalStatus OperationalStatus,
        IReadOnlyList<IPAddress> UnicastAddresses);

    private static NetworkInterfaceInfo ToNetworkInterfaceInfo(NetworkInterface networkInterface)
    {
        IReadOnlyList<IPAddress> addresses;
        try
        {
            addresses = networkInterface
                .GetIPProperties()
                .UnicastAddresses
                .Select(x => x.Address)
                .ToArray();
        }
        catch
        {
            addresses = Array.Empty<IPAddress>();
        }

        return new NetworkInterfaceInfo(
            networkInterface.Name,
            networkInterface.Description,
            networkInterface.OperationalStatus,
            addresses);
    }

    private static IReadOnlyList<string> NormalizeUrls(IEnumerable<string>? urls)
    {
        return urls.EmptyIfNull()
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(NormalizeUrl)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<IPAddress> NormalizeWslHostAddresses(IEnumerable<IPAddress>? wslHostAddresses)
    {
        var result = new List<IPAddress>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var hostAddress in wslHostAddresses.EmptyIfNull())
        {
            if (!IsUsableHostAddress(hostAddress))
            {
                continue;
            }

            if (seen.Add(hostAddress.ToReadableString()))
            {
                result.Add(hostAddress);
            }
        }

        return result;
    }

    private static void AddExpandedUrl(ICollection<string> expandedUrls, ISet<string> seen, string url)
    {
        var normalizedUrl = NormalizeUrl(url);
        if (seen.Add(normalizedUrl))
        {
            expandedUrls.Add(normalizedUrl);
        }
    }

    private static string NormalizeUrl(string url)
    {
        return url.Trim().TrimEnd('/');
    }

    private static Uri? TryParseAbsoluteUri(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri : null;
    }
}
