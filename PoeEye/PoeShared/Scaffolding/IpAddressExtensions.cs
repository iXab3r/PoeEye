using System;
using System.Net;

namespace PoeShared.Scaffolding
{
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
    }
}