using System.Collections;
using System.Net;
using System.Net.Sockets;

namespace PoeShared.Scaffolding;

public static class IpAddressExtensions
{
    public static string ToReadableIpAddressOrIdentity(this string ip)
    {
        if (TryParseToIpAddress(ip, out var ipAddress))
        {
            return ipAddress.ToReadableString();
        }

        return ip;
    }
        
    public static bool TryParseToIpAddress(this string ip, out IPAddress result)
    {
        IPAddress ipAddress;
        if (Uri.TryCreate(ip, UriKind.RelativeOrAbsolute, out var uri) && uri.IsAbsoluteUri && IPAddress.TryParse(uri.AbsolutePath, out var ipFromUri))
        {
            ipAddress = ipFromUri;
        }  else if (IPAddress.TryParse(ip, out var ipFromAddress))
        {
            ipAddress = ipFromAddress;
        }
        else
        {
            ipAddress = default;
        }

        result = ipAddress;
        return result != null;
    }
        
    public static string ToReadableString(this IPAddress ipAddress)
    {
        if (ipAddress == null)
        {
            return default;
        }
        return ipAddress.IsIPv4MappedToIPv6 ? ipAddress.MapToIPv4().ToString() : ipAddress.ToString();
    }


    public static bool IsInSubnet(this IPAddress address, string subnetMask)
    {
        if (IsInSubnetInternal(address, subnetMask))
        {
            return true;
        }

        if (!address.IsIPv4MappedToIPv6)
        {
            return false;
        }

        var ipv4 = address.MapToIPv4();
        return IsInSubnetInternal(ipv4, subnetMask);

    }

    private static bool IsInSubnetInternal(IPAddress address, string subnetMask)
    {
        var slashIdx = subnetMask.IndexOf("/", StringComparison.Ordinal);
        if (slashIdx == -1)
        { // We only handle netmasks in format "IP/PrefixLength".
            throw new NotSupportedException("Only SubNetMasks with a given prefix length are supported.");
        }

        // First parse the address of the netmask before the prefix length.
        var maskAddress = IPAddress.Parse(subnetMask.Substring(0, slashIdx));

        if (maskAddress.AddressFamily != address.AddressFamily)
        { // We got something like an IPV4-Address for an IPv6-Mask. This is not valid.
            return false;
        }

        // Now find out how long the prefix is.
        int maskLength = int.Parse(subnetMask.Substring(slashIdx + 1));

        if (maskAddress.AddressFamily == AddressFamily.InterNetwork)
        {
            // Convert the mask address to an unsigned integer.
            var maskAddressBits = BitConverter.ToUInt32(maskAddress.GetAddressBytes().AsEnumerable().Reverse().ToArray(), 0);

            // And convert the IpAddress to an unsigned integer.
            var ipAddressBits = BitConverter.ToUInt32(address.GetAddressBytes().AsEnumerable().Reverse().ToArray(), 0);

            // Get the mask/network address as unsigned integer.
            uint mask = uint.MaxValue << (32 - maskLength);

            // https://stackoverflow.com/a/1499284/3085985
            // Bitwise AND mask and MaskAddress, this should be the same as mask and IpAddress
            // as the end of the mask is 0000 which leads to both addresses to end with 0000
            // and to start with the prefix.
            return (maskAddressBits & mask) == (ipAddressBits & mask);
        }

        if (maskAddress.AddressFamily == AddressFamily.InterNetworkV6)
        {
            // Convert the mask address to a BitArray.
            var maskAddressBits = new BitArray(maskAddress.GetAddressBytes());

            // And convert the IpAddress to a BitArray.
            var ipAddressBits = new BitArray(address.GetAddressBytes());

            if (maskAddressBits.Length != ipAddressBits.Length)
            {
                throw new ArgumentException("Length of IP Address and Subnet Mask do not match.");
            }

            // Compare the prefix bits.
            for (int maskIndex = 0; maskIndex < maskLength; maskIndex++)
            {
                if (ipAddressBits[maskIndex] != maskAddressBits[maskIndex])
                {
                    return false;
                }
            }

            return true;
        }

        throw new NotSupportedException("Only InterNetworkV6 or InterNetwork address families are supported.");
    }
}