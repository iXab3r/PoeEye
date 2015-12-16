namespace ProxyProvider.ProxiesSource.FoxTools
{
    using System;
    using System.Collections.Generic;
    using System.Net;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    using RestEase;

    internal sealed class FoxToolsProxiesSource : IProxiesSource 
    {
        public IEnumerable<IWebProxy> GetProxies()
        {
            var jsonSettings = new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
            };

            var client = new RestClient("http://api.foxtools.ru/v2")
            {
                ResponseDeserializer = new JsonResponseDeserializer(),
                JsonSerializerSettings = jsonSettings,
            }.For<IFoxToolsApi>();

            var proxies = client.GetProxiesList(type: FoxProxyType.HTTP, limit: 500, maxUptimeInSeconds:5, countryISOCode: "iso3166a2" /* Russia */  ).Result;

            if (proxies?.Response?.Items == null)
            {
                yield break;
            }

            foreach (var proxy in proxies.Response.Items)
            {
                if (proxy.Available == FoxProxyYesNoAny.No)
                {
                    continue;
                }

                if (proxy.Type != FoxProxyType.HTTP && proxy.Type != FoxProxyType.All)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(proxy.Address))
                {
                    continue;
                }

                Uri proxyUri;

                if (!Uri.TryCreate($"http://{proxy.Address}:{proxy.Port}", UriKind.RelativeOrAbsolute, out proxyUri))
                {
                    continue;
                }

                yield return new WrappedProxy(proxyUri, $"uptime: {proxy.Uptime}, country: {proxy.Country?.NameEn}");
            }
        }
    }
}