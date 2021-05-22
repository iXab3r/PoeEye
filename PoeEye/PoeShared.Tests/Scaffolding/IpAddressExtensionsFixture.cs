using System.Net;
using NUnit.Framework;
using PoeShared.Scaffolding;
using Shouldly;

namespace PoeShared.Tests.Scaffolding
{
    [TestFixture]
    public class IpAddressExtensionsFixture
    {
        [TestCase("192.168.5.85/24", "192.168.5.1")]
        [TestCase("192.168.5.85/24", "192.168.5.254")]
        [TestCase("10.128.240.50/30", "10.128.240.48")]
        [TestCase("10.128.240.50/30", "10.128.240.49")]
        [TestCase("10.128.240.50/30", "10.128.240.50")]
        [TestCase("10.128.240.50/30", "10.128.240.51")]
        public void IpV4SubnetMaskMatchesValidIpAddress(string netMask, string ipAddress)
        {
            var ipAddressObj = IPAddress.Parse(ipAddress);
            Assert.True(ipAddressObj.IsInSubnet(netMask));
        }

        [TestCase("192.168.5.85/24", "192.168.4.254")]
        [TestCase("192.168.5.85/24", "191.168.5.254")]
        [TestCase("10.128.240.50/30", "10.128.240.47")]
        [TestCase("10.128.240.50/30", "10.128.240.52")]
        [TestCase("10.128.240.50/30", "10.128.239.50")]
        [TestCase("10.128.240.50/30", "10.127.240.51")]
        public void IpV4SubnetMaskDoesNotMatchInvalidIpAddress(string netMask, string ipAddress)
        {
            var ipAddressObj = IPAddress.Parse(ipAddress);
            Assert.False(ipAddressObj.IsInSubnet(netMask));
        }

        [TestCase("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0012:0000:0000:0000:0000")]
        [TestCase("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0012:FFFF:FFFF:FFFF:FFFF")]
        [TestCase("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0012:0001:0000:0000:0000")]
        [TestCase("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0012:FFFF:FFFF:FFFF:FFF0")]
        [TestCase("2001:db8:abcd:0012::0/128", "2001:0DB8:ABCD:0012:0000:0000:0000:0000")]
        public void IpV6SubnetMaskMatchesValidIpAddress(string netMask, string ipAddress)
        {
            var ipAddressObj = IPAddress.Parse(ipAddress);
            Assert.True(ipAddressObj.IsInSubnet(netMask));
        }

        [TestCase("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0011:FFFF:FFFF:FFFF:FFFF")]
        [TestCase("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0013:0000:0000:0000:0000")]
        [TestCase("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0013:0001:0000:0000:0000")]
        [TestCase("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0011:FFFF:FFFF:FFFF:FFF0")]
        [TestCase("2001:db8:abcd:0012::0/128", "2001:0DB8:ABCD:0012:0000:0000:0000:0001")]
        public void IpV6SubnetMaskDoesNotMatchInvalidIpAddress(string netMask, string ipAddress)
        {
            var ipAddressObj = IPAddress.Parse(ipAddress);
            Assert.False(ipAddressObj.IsInSubnet(netMask));
        }

        [TestCase("77.75.153.0/25", "::ffff:77.75.154.203", false)]
        [TestCase("77.75.154.128/25", "::ffff:77.75.154.203", true)]
        public void Ipv6v4(string netMask, string ipAddress, bool expected)
        {
            var ipAddressObj = IPAddress.Parse(ipAddress);
            ipAddressObj.IsInSubnet(netMask).ShouldBe(expected);
        }
    }
}