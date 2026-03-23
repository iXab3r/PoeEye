using System.Net;
using System.Net.NetworkInformation;
using NUnit.Framework;
using PoeShared.Scaffolding;
using Shouldly;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
public sealed class NetworkUtilsFixture
{
    [Test]
    public void ShouldReturnOnlyActiveWslIpv4HostAddresses()
    {
        var result = NetworkUtils.GetWslHostAddresses(
            new[]
            {
                new NetworkUtils.NetworkInterfaceInfo(
                    "vEthernet (WSL)",
                    "Hyper-V Virtual Ethernet Adapter",
                    OperationalStatus.Up,
                    new[] { IPAddress.Loopback, IPAddress.IPv6Loopback, IPAddress.Parse("172.19.208.1") }),
                new NetworkUtils.NetworkInterfaceInfo(
                    "Ethernet",
                    "Intel Ethernet Adapter",
                    OperationalStatus.Up,
                    new[] { IPAddress.Parse("192.168.1.10") }),
                new NetworkUtils.NetworkInterfaceInfo(
                    "vEthernet (WSL)",
                    "Hyper-V Virtual Ethernet Adapter",
                    OperationalStatus.Down,
                    new[] { IPAddress.Parse("172.19.208.2") })
            });

        result.ShouldBe(new[] { IPAddress.Parse("172.19.208.1") });
    }

    [Test]
    public void ShouldMatchWslInterfacesByDescriptionAndRemoveDuplicateAddresses()
    {
        var result = NetworkUtils.GetWslHostAddresses(
            new[]
            {
                new NetworkUtils.NetworkInterfaceInfo(
                    "vEthernet (Default Switch)",
                    "WSL (Hyper-V firewall)",
                    OperationalStatus.Up,
                    new[] { IPAddress.Parse("172.19.208.1") }),
                new NetworkUtils.NetworkInterfaceInfo(
                    "vEthernet (WSL)",
                    "Hyper-V Virtual Ethernet Adapter",
                    OperationalStatus.Up,
                    new[] { IPAddress.Parse("172.19.208.1"), IPAddress.Parse("172.19.208.2") })
            });

        result.ShouldBe(
            new[]
            {
                IPAddress.Parse("172.19.208.1"),
                IPAddress.Parse("172.19.208.2")
            });
    }

    [Test]
    public void ShouldExpandWslHostAliasesIntoConcreteUrls()
    {
        var result = NetworkUtils.ExpandWslHostAliases(
            new[]
            {
                "http://127.0.0.1:49080",
                "http://localhost-wsl:49080",
                "https://localhost-wsl:49443"
            },
            new[] { IPAddress.Parse("172.19.208.1") });

        result.ConfiguredUrls.ShouldBe(
            new[]
            {
                "http://127.0.0.1:49080",
                "http://localhost-wsl:49080",
                "https://localhost-wsl:49443"
            });

        result.ExpandedUrls.ShouldBe(
            new[]
            {
                "http://127.0.0.1:49080",
                "http://172.19.208.1:49080",
                "https://172.19.208.1:49443"
            });

        result.ExpandedAliasUrls.ShouldBe(
            new[]
            {
                "http://localhost-wsl:49080",
                "https://localhost-wsl:49443"
            });

        result.SkippedAliasUrls.ShouldBe(new string[0]);
    }

    [Test]
    public void ShouldSkipWslHostAliasesWhenNoAddressesAreAvailable()
    {
        var result = NetworkUtils.ExpandWslHostAliases(
            new[]
            {
                "http://localhost-wsl:49080",
                "https://127.0.0.1:49443"
            },
            new IPAddress[0]);

        result.ExpandedUrls.ShouldBe(new[] { "https://127.0.0.1:49443" });
        result.ExpandedAliasUrls.ShouldBe(new string[0]);
        result.SkippedAliasUrls.ShouldBe(new[] { "http://localhost-wsl:49080" });
    }
}
