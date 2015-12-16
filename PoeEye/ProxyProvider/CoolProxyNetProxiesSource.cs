namespace ProxyProvider
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;

    using CsQuery;
    using CsQuery.ExtensionMethods.Internal;

    internal sealed class CoolProxyNetProxiesSource : IProxiesSource
    {
        public static readonly int PagesToEnumerate = 3;

        public IEnumerable<IWebProxy> GetProxies()
        {
            var result = new List<IWebProxy>();

            Log.Instance.Debug($"[CoolProxyNetProxiesSource.GetProxies] Enumerating proxies...");
            foreach (var pageUri in GetPagesUris().Take(PagesToEnumerate))
            {
                try
                {
                    var cq = CQ.CreateFromUrl(pageUri);

                    var proxiesRows = cq["table tr"].Select(x => x.Render());

                    foreach (var proxyRow in proxiesRows)
                    {
                        IWebProxy proxy;
                        if (!TryToParseProxy(proxyRow, out proxy))
                        {
                            continue;
                        }

                        result.Add(proxy);
                    }
                }
                catch (Exception ex)
                {
                    continue;
                }
            }
            Log.Instance.Debug($"[CoolProxyNetProxiesSource.GetProxies] Proxies list: \r\n\t{string.Join("\r\n\t", result)}");

            return result;
        }

        private IEnumerable<string> GetPagesUris()
        {
            var firstPageUri = @"http://www.cool-proxy.net/proxies/http_proxy_list/sort:response_time_average/direction:asc/page:1";

            yield return firstPageUri;

            var cq = CQ.CreateFromUrl(firstPageUri);

            foreach (var pageUri in cq["span a"]
                .Select(x => x.GetAttribute("href"))
                .Where(pageUri => !string.IsNullOrWhiteSpace(pageUri))
                .Select(pageUri => @"http://www.cool-proxy.net" + pageUri))
            {
                yield return pageUri;
            }
        }

        private bool TryToParseProxy(string proxyHtmlRow, out IWebProxy proxy)
        {
            proxy = null;


            var cq = CQ.Create(proxyHtmlRow);

            var portString = cq["tr td:nth-child(2)"].Text();

            int port;
            if (!Int32.TryParse(portString, out port))
            {
                return false;
            }

            var encodedHostnameString = cq["tr td:nth-child(1)"].Text();
            if (string.IsNullOrWhiteSpace(encodedHostnameString))
            {
                return false;
            }

            var hostname = DecodeProxyHostname(encodedHostnameString);
            if (string.IsNullOrWhiteSpace(hostname))
            {
                return false;
            }

            Uri proxyUri;
            if (!Uri.TryCreate($"http://{hostname}:{port}", UriKind.RelativeOrAbsolute,  out proxyUri))
            {
                return false;
            }

            proxy = new WrappedProxy(proxyUri);
            return true;
        }

        private string DecodeProxyHostname(string encodedHostname)
        {
            var encodedHostnameRegex = new Regex(@"str_rot13\(\""(.*?)\""\)");
            var encodedHostnameMatch = encodedHostnameRegex.Match(encodedHostname);

            if (!encodedHostnameMatch.Success)
            {
                return string.Empty;
            }

            var encodedValue = encodedHostnameMatch.Groups[1].Value;
            var rotatedValue = StringRotate13(encodedValue);
            var resultRaw = Convert.FromBase64String(rotatedValue);
            var result = Encoding.Default.GetString(resultRaw);
            return result;
        }

        private string StringRotate13(string input)
        {
            var result = new StringBuilder();

            foreach (var c in input)
            {
                if (!Char.IsLetter(c))
                {
                    result.Append(c);
                }
                else
                {
                    result.Append(c.ToLower() < 'n' ? (char)(c + 13) : (char)(c - 13));
                }
            }
            return result.ToString();
        }
    }
}